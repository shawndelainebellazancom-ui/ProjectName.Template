using Microsoft.AspNetCore.Mvc;
using ProjectName.MakerService.Grpc;
using ProjectName.Shared.Models;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Maker Service.
/// </summary>
/// <param name="client">The gRPC client for the Maker service.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/maker")]
[Produces("application/json")]
public partial class MakerController(
    Maker.MakerClient client,
    ILogger<MakerController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "MAKER GATEWAY: Materializing Plan {PlanId}")]
    private partial void LogMakingRequest(string planId);

    /// <summary>
    /// Materializes an artifact from a plan (Direct Access).
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <returns>The created artifact.</returns>
    [HttpPost]
    public async Task<IActionResult> MakeArtifact([FromBody] Plan plan)
    {
        LogMakingRequest(plan.Id);

        var request = new MakeRequest { PlanId = plan.Id };
        request.Steps.AddRange(plan.Steps);
        foreach (var r in plan.Resources) request.Resources.Add(r.Key, r.Value);

        var reply = await client.MakeArtifactAsync(request);

        var artifact = new Artifact(
            reply.ArtifactId,
            plan.Id,
            reply.Content,
            reply.ArtifactType
        );

        return Ok(artifact);
    }
}