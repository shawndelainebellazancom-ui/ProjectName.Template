using Grpc.Core;
using ProjectName.ReflectorService.Grpc;

namespace ProjectName.ReflectorService.Services;

/// <summary>
/// Implements the Reflector gRPC service responsible for meta-cognitive analysis.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReflectorService"/> class.
/// </remarks>
/// <param name="logger">The logger for observability.</param>
public partial class ReflectorService(ILogger<ReflectorService> logger) : Reflector.ReflectorBase
{
    [LoggerMessage(
        EventId = 300,
        Level = LogLevel.Information,
        Message = "gRPC REFLECTOR: Analyzing cycle. Valid: {IsValid}, Score: {Score}")]
    private partial void LogAnalyzing(bool isValid, double score);

    /// <summary>
    /// Analyzes the results of a Make/Check cycle to determine convergence.
    /// </summary>
    /// <param name="request">The results from the Checker.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>A reflection on the cycle.</returns>
    public override Task<ReflectReply> AnalyzeCycle(ReflectRequest request, ServerCallContext context)
    {
        LogAnalyzing(request.IsValid, request.ConfidenceScore);

        var reply = new ReflectReply();

        if (request.IsValid)
        {
            reply.Insight = "Cycle converged. Artifact is production ready.";
            reply.OptimizedIntent = ""; // Empty means done
        }
        else
        {
            reply.Insight = "Cycle failed validation. Intent refined for clarity.";
            reply.OptimizedIntent = "Refined Intent: Ensure artifact content is not empty.";
        }

        return Task.FromResult(reply);
    }
}