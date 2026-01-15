using System.ComponentModel; // For [Description]
using ModelContextProtocol.Server;
using ProjectName.PlannerService.Grpc;
using ProjectName.MakerService.Grpc;
using ProjectName.CheckerService.Grpc;
using ProjectName.ReflectorService.Grpc;
using System.Text;
using System.Globalization;

namespace ProjectName.McpServer.Tools;

[McpServerToolType]
public partial class McpToolBase(
    Planner.PlannerClient planner,
    Maker.MakerClient maker,
    Checker.CheckerClient checker,
    Reflector.ReflectorClient reflector,
    ILogger<McpToolBase> logger)
{
    // ------------------------------------------------------------
    // Logging
    // ------------------------------------------------------------

    [LoggerMessage(Level = LogLevel.Information, Message = "MCP: Planning intent '{Intent}'")]
    private partial void LogPlanning(string intent);

    [LoggerMessage(Level = LogLevel.Information, Message = "MCP: Making artifact for Plan '{PlanId}'")]
    private partial void LogMaking(string planId);

    [LoggerMessage(Level = LogLevel.Information, Message = "MCP: Checking artifact '{ArtifactId}'")]
    private partial void LogChecking(string artifactId);

    [LoggerMessage(Level = LogLevel.Information, Message = "MCP: Reflecting on cycle (Valid: {Valid})")]
    private partial void LogReflecting(bool valid);

    // ------------------------------------------------------------
    // PLAN
    // ------------------------------------------------------------

    [McpServerTool(Name = "plan")]
    public async Task<string> Plan(
        [Description("The goal or intent")] string intent)
    {
        LogPlanning(intent);

        var request = new IntentRequest
        {
            Id = Guid.NewGuid().ToString(),
            Content = intent
        };

        var reply = await planner.CreatePlanAsync(request);

        var sb = new StringBuilder();

        sb.AppendFormat(CultureInfo.InvariantCulture, "Plan ID: {0}", reply.Id).AppendLine();
        sb.AppendLine("Steps:");

        foreach (var step in reply.Steps)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "- {0}", step).AppendLine();
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------
    // MAKE
    // ------------------------------------------------------------

    [McpServerTool(Name = "make")]
    public async Task<string> Make(
        [Description("The Plan ID")] string planId)
    {
        LogMaking(planId);

        var request = new MakeRequest { PlanId = planId };
        request.Steps.Add("Execute Plan");

        var reply = await maker.MakeArtifactAsync(request);

        return $"Artifact Created: {reply.ArtifactId} ({reply.ArtifactType})";
    }

    // ------------------------------------------------------------
    // CHECK
    // ------------------------------------------------------------

    [McpServerTool(Name = "check")]
    public async Task<string> Check(
        [Description("The Artifact ID")] string artifactId)
    {
        LogChecking(artifactId);

        var request = new CheckRequest
        {
            ArtifactId = artifactId,
            Content = "Placeholder Content",
            ArtifactType = "Unknown"
        };

        var reply = await checker.ValidateArtifactAsync(request);

        return reply.IsValid
            ? $"Valid (Score: {reply.ConfidenceScore})"
            : $"Invalid: {string.Join(", ", reply.Issues)}";
    }

    // ------------------------------------------------------------
    // REFLECT
    // ------------------------------------------------------------

    [McpServerTool(Name = "reflect")]
    public async Task<string> Reflect(
        [Description("Is Valid")] bool isValid,
        [Description("Confidence Score")] double score)
    {
        LogReflecting(isValid);

        var request = new ReflectRequest
        {
            IsValid = isValid,
            ConfidenceScore = score
        };

        var reply = await reflector.AnalyzeCycleAsync(request);

        return string.IsNullOrEmpty(reply.OptimizedIntent)
            ? "Converged"
            : $"Iterating. refined_intent: {reply.OptimizedIntent}";
    }
}
