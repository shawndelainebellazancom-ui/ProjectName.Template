using Grpc.Core;
using Microsoft.Agents.AI;
using ProjectName.MakerService.Grpc;
using System.Globalization;
using System.Text;

namespace ProjectName.MakerService.Services;

public partial class MakerService(
    AIAgent makerAgent,
    ILogger<MakerService> logger) : Maker.MakerBase
{
    // --- HIGH PERFORMANCE LOGGING ---
    [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "gRPC MAKER: Received Plan {PlanId}. Delegating to Agent...")]
    private partial void LogPlanReceived(string planId);

    [LoggerMessage(EventId = 101, Level = LogLevel.Error, Message = "Agent Generation Failed")]
    private partial void LogAgentFailure(Exception ex);

    public override async Task<MakeReply> MakeArtifact(MakeRequest request, ServerCallContext context)
    {
        LogPlanReceived(request.PlanId);

        // 1. CONSTRUCT THE CONTEXT
        var prompt = new StringBuilder();

        // Fix CA1305: Use InvariantCulture
        prompt.AppendFormat(CultureInfo.InvariantCulture, "--- EXECUTION PLAN (ID: {0}) ---", request.PlanId);
        prompt.AppendLine();

        foreach (var step in request.Steps)
        {
            prompt.AppendFormat(CultureInfo.InvariantCulture, "STEP: {0}\n", step);
        }
        prompt.AppendLine();
        prompt.AppendFormat(CultureInfo.InvariantCulture, "OUTPUT LANGUAGE: {0}\n", request.ArtifactType ?? "Code");

        if (request.Resources.Count > 0)
        {
            prompt.AppendLine("CONTEXT:");
            foreach (var r in request.Resources)
            {
                prompt.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\n", r.Key, r.Value);
            }
        }

        try
        {
            // 2. INVOKE THE AGENT
            // The Agent Framework handles the message history, system prompt, and response parsing.
            var response = await makerAgent.RunAsync(prompt.ToString(), cancellationToken: context.CancellationToken);

            // 3. EXTRACT CONTENT
            // The AgentRunResponse.ToString() usually returns the text, or we access the last message.
            var content = response.ToString();

            // 4. SANITIZE
            content = CleanArtifact(content);

            // 5. RETURN REALITY
            return new MakeReply
            {
                ArtifactId = Guid.NewGuid().ToString(),
                PlanId = request.PlanId,
                Content = content,
                ArtifactType = request.ArtifactType ?? "Text/Code",
                Success = true
            };
        }
        catch (Exception ex)
        {
            LogAgentFailure(ex);
            return new MakeReply
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static string CleanArtifact(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Fix CA1310: Use StringComparison.Ordinal
        var lines = input.Split('\n').ToList();

        // Remove start fence (```python or just ```)
        if (lines.Count > 0 && lines[0].Trim().StartsWith("```", StringComparison.Ordinal))
            lines.RemoveAt(0);

        // Remove end fence
        if (lines.Count > 0 && lines[^1].Trim().StartsWith("```", StringComparison.Ordinal))
            lines.RemoveAt(lines.Count - 1);

        return string.Join("\n", lines).Trim();
    }
}
