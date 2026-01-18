using Microsoft.AspNetCore.Mvc;
using ProjectName.MakerService.Grpc;
using ProjectName.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Maker Service.
/// </summary>
[ApiController]
[Route("maker")]
[Produces("application/json")]
[SwaggerTag("Artifact materialization - converts execution plans into concrete deliverables")]
public partial class MakerController(
    Maker.MakerClient client,
    ILogger<MakerController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "MAKER GATEWAY: Materializing Plan {PlanId}")]
    private partial void LogMakingRequest(string planId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error materializing plan {PlanId}")]
    private partial void LogMaterializationError(Exception ex, string planId);

    /// <summary>
    /// Materializes an artifact from a plan (Direct Access).
    /// </summary>
    /// <remarks>
    /// The Maker service executes the plan's steps and produces a concrete artifact.
    /// This can be code, documentation, configuration files, or any other deliverable.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "id": "plan-456",
    ///   "originalIntentId": "intent-123",
    ///   "steps": [
    ///     "Pull Docker image",
    ///     "Configure container",
    ///     "Deploy to registry"
    ///   ],
    ///   "resources": {
    ///     "runtime": "docker",
    ///     "registry": "ghcr.io"
    ///   }
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "id": "artifact-789",
    ///   "planId": "plan-456",
    ///   "content": "#!/bin/bash\ndocker pull nginx:latest\ndocker tag nginx:latest ghcr.io/myorg/nginx:v1",
    ///   "artifactType": "Script/Bash"
    /// }
    /// ```
    /// </remarks>
    /// <param name="plan">The execution plan to materialize.</param>
    /// <returns>The created artifact with content and metadata.</returns>
    /// <response code="200">Artifact created successfully.</response>
    /// <response code="400">Invalid plan (missing steps or resources).</response>
    /// <response code="500">Materialization failed.</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create Artifact from Plan",
        Description = "Executes a plan and materializes it into a concrete artifact",
        OperationId = "MakeArtifact",
        Tags = new[] { "Materialization" }
    )]
    [SwaggerResponse(200, "Artifact created successfully", typeof(Artifact))]
    [SwaggerResponse(400, "Invalid plan")]
    [SwaggerResponse(500, "Materialization error")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<ActionResult<Artifact>> MakeArtifact(
        [FromBody, SwaggerRequestBody("The plan to execute and materialize", Required = true)] Plan plan)
    {
        if (plan == null || string.IsNullOrWhiteSpace(plan.Id))
        {
            return BadRequest(new { error = "Plan ID cannot be empty" });
        }

        if (plan.Steps == null || plan.Steps.Count == 0)
        {
            return BadRequest(new { error = "Plan must contain at least one step" });
        }

        LogMakingRequest(plan.Id);

        try
        {
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
        catch (Exception ex)
        {
            LogMaterializationError(ex, plan.Id);
            return StatusCode(500, new { error = "Artifact creation failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Get maker service health status.
    /// </summary>
    /// <remarks>
    /// Returns the operational status of the Maker service.
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "status": "healthy",
    ///   "service": "maker",
    ///   "timestamp": "2026-01-17T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <returns>Service health information.</returns>
    /// <response code="200">Service is operational.</response>
    [HttpGet("health")]
    [SwaggerOperation(
        Summary = "Maker Health Check",
        Description = "Verify the Maker service is accessible and operational",
        OperationId = "MakerHealth",
        Tags = new[] { "Diagnostics" }
    )]
    [SwaggerResponse(200, "Service is healthy")]
    [Produces("application/json")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "maker",
            timestamp = DateTime.UtcNow
        });
    }
}
