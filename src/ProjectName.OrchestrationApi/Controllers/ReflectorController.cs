using Microsoft.AspNetCore.Mvc;
using ProjectName.ReflectorService.Grpc;
using ProjectName.Shared.Models;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Reflector Service.
/// </summary>
/// <param name="client">The gRPC client for the Reflector service.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/reflector")]
[Produces("application/json")]
public partial class ReflectorController(
    Reflector.ReflectorClient client,
    ILogger<ReflectorController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "REFLECTOR GATEWAY: Analyzing validation results")]
    private partial void LogReflectRequest();

    /// <summary>
    /// Analyzes validation results for meta-cognition (Direct Access).
    /// </summary>
    /// <param name="validation">The validation report.</param>
    /// <returns>The reflection insight.</returns>
    [HttpPost]
    public async Task<IActionResult> Reflect([FromBody] Validation validation)
    {
        LogReflectRequest();

        var request = new ReflectRequest
        {
            IsValid = validation.IsValid,
            ConfidenceScore = validation.ConfidenceScore
        };
        request.Issues.AddRange(validation.Issues);

        var reply = await client.AnalyzeCycleAsync(request);

        var reflection = new Reflection(
            reply.Insight,
            reply.OptimizedIntent
        );

        return Ok(reflection);
    }
}