using Microsoft.AspNetCore.Mvc;
using ProjectName.CheckerService.Grpc;
using ProjectName.Shared.Models;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Checker Service.
/// </summary>
/// <param name="client">The gRPC client for the Checker service.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/checker")]
[Produces("application/json")]
public partial class CheckerController(
    Checker.CheckerClient client,
    ILogger<CheckerController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "CHECKER GATEWAY: Validating Artifact {Id}")]
    private partial void LogCheckRequest(string id);

    /// <summary>
    /// Validates an artifact (Direct Access).
    /// </summary>
    /// <param name="artifact">The artifact to check.</param>
    /// <returns>The validation report.</returns>
    [HttpPost]
    public async Task<IActionResult> CheckArtifact([FromBody] Artifact artifact)
    {
        LogCheckRequest(artifact.Id);

        var request = new CheckRequest
        {
            ArtifactId = artifact.Id,
            Content = artifact.Content,
            ArtifactType = artifact.ArtifactType
        };

        var reply = await client.ValidateArtifactAsync(request);

        var validation = new Validation(
            reply.IsValid,
            reply.Issues.ToArray(),
            reply.ConfidenceScore
        );

        return Ok(validation);
    }
}