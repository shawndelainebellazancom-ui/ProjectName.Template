using Grpc.Core;
using ProjectName.CheckerService.Grpc;

namespace ProjectName.CheckerService.Services;

/// <summary>
/// Implements the Checker gRPC service responsible for validating artifacts.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CheckerService"/> class.
/// </remarks>
/// <param name="logger">The logger for observability.</param>
public partial class CheckerService(ILogger<CheckerService> logger) : Checker.CheckerBase
{
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Information,
        Message = "gRPC CHECKER: Validating Artifact {ArtifactId} ({ArtifactType})")]
    private partial void LogValidatingArtifact(string artifactId, string artifactType);

    /// <summary>
    /// Validates an artifact against quality constraints.
    /// </summary>
    /// <param name="request">The artifact data to validate.</param>
    /// <param name="context">The gRPC context.</param>
    /// <returns>A validation report.</returns>
    public override Task<CheckReply> ValidateArtifact(CheckRequest request, ServerCallContext context)
    {
        LogValidatingArtifact(request.ArtifactId, request.ArtifactType);

        // Logic stub
        bool isValid = !string.IsNullOrWhiteSpace(request.Content);

        var reply = new CheckReply
        {
            IsValid = isValid,
            ConfidenceScore = isValid ? 99.9 : 0.0
        };

        if (!isValid) reply.Issues.Add("Content was empty");

        return Task.FromResult(reply);
    }
}