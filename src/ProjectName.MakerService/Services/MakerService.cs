using Grpc.Core;
using ProjectName.MakerService.Grpc;

namespace ProjectName.MakerService.Services;

/// <summary>
/// Implements the Maker gRPC service responsible for materializing artifacts from execution plans.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MakerService"/> class.
/// </remarks>
/// <param name="logger">The logger for observability.</param>
public partial class MakerService(ILogger<MakerService> logger) : Maker.MakerBase
{
    // --- Source Generated Logger ---
    // This generates highly optimized code at compile time (Zero Allocation)
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "gRPC MAKER: Received Plan {PlanId} for materialization")]
    private partial void LogPlanReceived(string planId);

    /// <summary>
    /// Generates an artifact based on the provided make request.
    /// </summary>
    /// <param name="request">The request containing plan ID, steps, and resources.</param>
    /// <param name="context">The gRPC server call context.</param>
    /// <returns>A reply containing the generated artifact details.</returns>
    public override Task<MakeReply> MakeArtifact(MakeRequest request, ServerCallContext context)
    {
        // Use the optimized logger delegate
        LogPlanReceived(request.PlanId);

        // Simulation Logic
        return Task.FromResult(new MakeReply
        {
            ArtifactId = Guid.NewGuid().ToString(),
            Content = $"// Generated Artifact for Plan {request.PlanId}\n// Steps processed: {request.Steps.Count}",
            ArtifactType = "Code/CSharp"
        });
    }
}