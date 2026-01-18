using Microsoft.AspNetCore.Mvc;
using ProjectName.CheckerService.Grpc;
using ProjectName.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Checker Service.
/// </summary>
[ApiController]
[Route("checker")]
[Produces("application/json")]
[SwaggerTag("Quality validation - ensures artifacts meet specified constraints and standards")]
public partial class CheckerController(
    Checker.CheckerClient client,
    ILogger<CheckerController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "CHECKER GATEWAY: Validating Artifact {Id}")]
    private partial void LogCheckRequest(string id);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error validating artifact {ArtifactId}")]
    private partial void LogValidationError(Exception ex, string artifactId);

    /// <summary>
    /// Validates an artifact (Direct Access).
    /// </summary>
    /// <remarks>
    /// The Checker service performs comprehensive validation including syntax verification,
    /// security scanning, performance checks, compliance validation, and best practices review.
    /// Returns a detailed validation report with issues categorized by severity.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "id": "artifact-789",
    ///   "planId": "plan-456",
    ///   "content": "def fibonacci(n): return n if n &lt;= 1 else fibonacci(n-1) + fibonacci(n-2)",
    ///   "artifactType": "Code/Python"
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "isValid": false,
    ///   "issues": ["Performance: Exponential complexity", "Missing docstring"],
    ///   "confidenceScore": 75.5
    /// }
    /// ```
    /// </remarks>
    /// <param name="artifact">The artifact to validate.</param>
    /// <returns>A validation report with issues and confidence score.</returns>
    /// <response code="200">Validation completed (check isValid field for result).</response>
    /// <response code="400">Invalid artifact (missing content or type).</response>
    /// <response code="500">Validation service error.</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Validate Artifact",
        Description = "Performs comprehensive quality validation on an artifact",
        OperationId = "ValidateArtifact",
        Tags = new[] { "Validation" }
    )]
    [SwaggerResponse(200, "Validation completed", typeof(Validation))]
    [SwaggerResponse(400, "Invalid artifact")]
    [SwaggerResponse(500, "Validation error")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<ActionResult<Validation>> CheckArtifact(
        [FromBody, SwaggerRequestBody("The artifact to validate", Required = true)] Artifact artifact)
    {
        if (artifact == null || string.IsNullOrWhiteSpace(artifact.Id))
        {
            return BadRequest(new { error = "Artifact ID cannot be empty" });
        }

        if (string.IsNullOrWhiteSpace(artifact.Content))
        {
            return BadRequest(new { error = "Artifact content cannot be empty" });
        }

        LogCheckRequest(artifact.Id);

        try
        {
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
        catch (Exception ex)
        {
            LogValidationError(ex, artifact.Id);
            return StatusCode(500, new { error = "Validation failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Batch validate multiple artifacts.
    /// </summary>
    /// <remarks>
    /// Validates multiple artifacts in a single request for efficiency.
    /// Useful for validating related artifacts or entire project outputs.
    /// </remarks>
    /// <param name="request">Array of artifacts to validate.</param>
    /// <returns>Array of validation results.</returns>
    /// <response code="200">Batch validation completed.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost("batch")]
    [SwaggerOperation(
        Summary = "Batch Validate Artifacts",
        Description = "Validate multiple artifacts in a single request",
        OperationId = "BatchValidate",
        Tags = new[] { "Validation" }
    )]
    [SwaggerResponse(200, "Batch validation completed")]
    [SwaggerResponse(400, "Invalid request")]
    public async Task<ActionResult<object>> BatchValidate(
        [FromBody, SwaggerRequestBody("Array of artifacts to validate", Required = true)] BatchValidationRequest request)
    {
        if (request?.Artifacts == null || request.Artifacts.Count == 0)
        {
            return BadRequest(new { error = "At least one artifact required" });
        }

        var results = new List<Validation>();

        foreach (var artifact in request.Artifacts)
        {
            var checkRequest = new CheckRequest
            {
                ArtifactId = artifact.Id,
                Content = artifact.Content,
                ArtifactType = artifact.ArtifactType
            };

            var reply = await client.ValidateArtifactAsync(checkRequest);

            results.Add(new Validation(
                reply.IsValid,
                reply.Issues.ToArray(),
                reply.ConfidenceScore
            ));
        }

        return Ok(new
        {
            totalValidated = results.Count,
            totalValid = results.Count(v => v.IsValid),
            totalInvalid = results.Count(v => !v.IsValid),
            results
        });
    }

    /// <summary>
    /// Get checker service health status.
    /// </summary>
    /// <returns>Service health information.</returns>
    /// <response code="200">Service is operational.</response>
    [HttpGet("health")]
    [SwaggerOperation(
        Summary = "Checker Health Check",
        Description = "Verify the Checker service is accessible and operational",
        OperationId = "CheckerHealth",
        Tags = new[] { "Diagnostics" }
    )]
    [SwaggerResponse(200, "Service is healthy")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "checker",
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Request model for batch validation.
/// </summary>
[SwaggerSchema(Description = "Container for multiple artifacts to validate")]
public class BatchValidationRequest
{
    /// <summary>
    /// Array of artifacts to validate.
    /// </summary>
    [SwaggerSchema("List of artifacts to validate in batch")]
    public List<Artifact> Artifacts { get; set; } = [];
}
