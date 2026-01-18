using Grpc.Core;
using Microsoft.Agents.AI;
using ModelContextProtocol.Client;
using ProjectName.CheckerService.Grpc;
using System.Globalization;
using System.Reflection; // Required for the fix
using System.Text;

namespace ProjectName.CheckerService.Services;

public partial class CheckerService(
    AIAgent checkerAgent,
    McpClientFactory mcpClientFactory,
    ILogger<CheckerService> logger) : Checker.CheckerBase
{
    private static readonly char[] _lineSeparators = ['\n', '\r'];

    [LoggerMessage(EventId = 200, Level = LogLevel.Information, Message = "gRPC CHECKER: Validating Artifact {ArtifactId}")]
    private partial void LogValidatingArtifact(string artifactId);

    [LoggerMessage(EventId = 201, Level = LogLevel.Error, Message = "Validation Logic Failed")]
    private partial void LogValidationFailure(Exception ex);

    [LoggerMessage(EventId = 202, Level = LogLevel.Error, Message = "Failed to connect to MCP Server or execute tool.")]
    private partial void LogMcpFailure(Exception ex);

    public override async Task<CheckReply> ValidateArtifact(CheckRequest request, ServerCallContext context)
    {
        LogValidatingArtifact(request.ArtifactId);

        try
        {
            var forensicReport = await GatherForensicsAsync(request.ArtifactType, request.Content);

            var prompt = string.Format(
                CultureInfo.InvariantCulture,
                "ARTIFACT TYPE: {0}\n\n--- CONTENT ---\n{1}\n\n--- FORENSIC REPORT ---\n{2}",
                request.ArtifactType,
                request.Content,
                forensicReport
            );

            var response = await checkerAgent.RunAsync(prompt, cancellationToken: context.CancellationToken);
            var analysis = response.ToString();

            var lines = analysis.Split(_lineSeparators, StringSplitOptions.RemoveEmptyEntries)
                                .Select(l => l.Trim())
                                .ToList();

            bool isValid = false;
            var issues = new List<string>();

            if (lines.Count > 0)
            {
                var verdict = lines[0].ToUpperInvariant();
                if (verdict.Contains("INVALID", StringComparison.Ordinal))
                {
                    isValid = false;
                    issues.AddRange(lines.Skip(1));
                }
                else if (verdict.Contains("VALID", StringComparison.Ordinal))
                {
                    isValid = true;
                }
                else
                {
                    isValid = !analysis.Contains("error", StringComparison.OrdinalIgnoreCase);
                    issues.Add("Review: " + analysis);
                }
            }

            var reply = new CheckReply
            {
                IsValid = isValid,
                ConfidenceScore = isValid ? 95.0 : 40.0,
                Summary = isValid ? "Passed Checks" : "Failed Checks"
            };

            reply.Issues.AddRange(issues);
            return reply;
        }
        catch (Exception ex)
        {
            LogValidationFailure(ex);
            return new CheckReply { IsValid = false, Summary = "Internal Validation Error: " + ex.Message };
        }
    }

    private async Task<string> GatherForensicsAsync(string type, string content)
    {
        try
        {
            await using var mcpClient = await mcpClientFactory();

            if (type.Contains("C#", StringComparison.OrdinalIgnoreCase) || type.Contains("Code", StringComparison.OrdinalIgnoreCase))
            {
                var args = new Dictionary<string, object?> { ["code"] = content };
                var result = await mcpClient.CallToolAsync("build_code", args);

                return ExtractTextFromContent(result.Content?.FirstOrDefault());
            }
            else if (type.Contains("Url", StringComparison.OrdinalIgnoreCase) || type.Contains("Web", StringComparison.OrdinalIgnoreCase))
            {
                if (Uri.IsWellFormedUriString(content, UriKind.Absolute))
                {
                    var args = new Dictionary<string, object?> { ["url"] = content };
                    var result = await mcpClient.CallToolAsync("take_snapshot", args);

                    return ExtractTextFromContent(result.Content?.FirstOrDefault());
                }
            }

            return "No specific tools available for this artifact type.";
        }
        catch (Exception ex)
        {
            LogMcpFailure(ex);
            return $"Tool Execution Failed: {ex.Message}";
        }
    }

    private static string ExtractTextFromContent(object? contentBlock)
    {
        if (contentBlock == null) return "No content returned.";

        // Universal extractor: Checks for "Text" property via reflection to bypass namespace issues
        var textProp = contentBlock.GetType().GetProperty("Text");
        if (textProp != null)
        {
            return textProp.GetValue(contentBlock)?.ToString() ?? "Empty Text";
        }

        return contentBlock.ToString() ?? "Unknown Content";
    }
}
