using Microsoft.AspNetCore.Mvc;
using ProjectName.PlannerService.Grpc;
using ProjectName.Shared.Models;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Planner Service.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PlannerController"/>.
/// </remarks>
/// <param name="client">The gRPC client for the Planner service.</param>
/// <param name="logger">The logger.</param>
[ApiController]
[Route("api/planner")]
[Produces("application/json")]
public partial class PlannerController(
    Planner.PlannerClient client,
    ILogger<PlannerController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "PLANNER GATEWAY: Requesting plan for Intent {Id}")]
    private partial void LogPlanningRequest(string id);

    /// <summary>
    /// Decomposes an intent into a structured plan (Direct Access).
    /// </summary>
    /// <param name="intent">The seed intent.</param>
    /// <returns>The generated plan.</returns>
    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] Intent intent)
    {
        LogPlanningRequest(intent.Id);

        var request = new IntentRequest { Id = intent.Id, Content = intent.Content };
        foreach (var kvp in intent.Context) request.Context.Add(kvp.Key, kvp.Value);

        var reply = await client.CreatePlanAsync(request);

        var domainPlan = new Plan(
            reply.Id,
            reply.OriginalIntentId,
            reply.Steps.ToList(),
            reply.Resources.ToDictionary(k => k.Key, v => v.Value)
        );

        return Ok(domainPlan);
    }
}