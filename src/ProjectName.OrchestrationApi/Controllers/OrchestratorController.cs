using Microsoft.AspNetCore.Mvc;
using ProjectName.PlannerService.Grpc;
using ProjectName.MakerService.Grpc;
using ProjectName.CheckerService.Grpc;
using ProjectName.ReflectorService.Grpc;
using ProjectName.Shared.Models;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// The central nervous system of the agent. Orchestrates the flow of data between
/// Planner, Maker, Checker, and Reflector services via gRPC.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrchestratorController"/>.
/// </remarks>
/// <param name="planner">The gRPC client for planning.</param>
/// <param name="maker">The gRPC client for artifact generation.</param>
/// <param name="checker">The gRPC client for validation.</param>
/// <param name="reflector">The gRPC client for meta-cognition.</param>
/// <param name="logger">The system logger.</param>
[ApiController]
[Route("api/orchestrate")]
[Produces("application/json")]
public partial class OrchestratorController(
    Planner.PlannerClient planner,
    Maker.MakerClient maker,
    Checker.CheckerClient checker,
    Reflector.ReflectorClient reflector,
    ILogger<OrchestratorController> logger) : ControllerBase
{
    // --- Source Generated Logging ---
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "ORCHESTRATOR: Received Intent: {Id}")]
    private partial void LogReceivedIntent(string id);

    /// <summary>
    /// Executes a full cognitive cycle based on a seed intent.
    /// </summary>
    /// <remarks>
    /// This endpoint implements the PMCR-O loop:
    /// 1. **Plan:** Converts intent to strategy.
    /// 2. **Make:** Converts strategy to artifact.
    /// 3. **Check:** Validates the artifact.
    /// 4. **Reflect:** Determines if convergence is reached.
    /// </remarks>
    /// <param name="intent">The input intent containing the goal and context.</param>
    /// <returns>The result of the cycle, indicating convergence or iteration.</returns>
    [HttpPost("run-cycle")]
    public async Task<IActionResult> ExecuteCycle([FromBody] Intent intent)
    {
        // Optimized logging call
        LogReceivedIntent(intent.Id);

        // 1. PLAN (gRPC)
        var planRequest = new IntentRequest { Id = intent.Id, Content = intent.Content };
        foreach (var kvp in intent.Context) planRequest.Context.Add(kvp.Key, kvp.Value);

        var protoPlan = await planner.CreatePlanAsync(planRequest);

        var domainPlan = new Plan(
            protoPlan.Id,
            protoPlan.OriginalIntentId,
            protoPlan.Steps.ToList(),
            protoPlan.Resources.ToDictionary(k => k.Key, v => v.Value)
        );

        // 2. MAKE (gRPC)
        // Map Domain Plan -> Proto MakeRequest
        var makeRequest = new MakeRequest { PlanId = domainPlan.Id };
        makeRequest.Steps.AddRange(domainPlan.Steps);
        foreach (var r in domainPlan.Resources) makeRequest.Resources.Add(r.Key, r.Value);

        var makeReply = await maker.MakeArtifactAsync(makeRequest);

        var artifact = new Artifact(
            makeReply.ArtifactId,
            domainPlan.Id,
            makeReply.Content,
            makeReply.ArtifactType
        );

        // 3. CHECK (gRPC)
        var checkRequest = new CheckRequest
        {
            ArtifactId = artifact.Id,
            Content = artifact.Content,
            ArtifactType = artifact.ArtifactType
        };

        var checkReply = await checker.ValidateArtifactAsync(checkRequest);

        var validation = new Validation(
            checkReply.IsValid,
            checkReply.Issues.ToArray(),
            checkReply.ConfidenceScore
        );

        // 4. REFLECT (gRPC)
        var reflectRequest = new ReflectRequest
        {
            IsValid = validation.IsValid,
            ConfidenceScore = validation.ConfidenceScore
        };
        reflectRequest.Issues.AddRange(validation.Issues);

        var reflectReply = await reflector.AnalyzeCycleAsync(reflectRequest);

        var reflection = new Reflection(
            reflectReply.Insight,
            reflectReply.OptimizedIntent
        );

        // 5. Result
        return Ok(new CycleResult(
            Status: validation.IsValid ? "Converged" : "Iterating",
            Artifact: artifact,
            Reflection: reflection
        ));
    }
}

/// <summary>
/// Encapsulates the outcome of a cognitive cycle.
/// </summary>
/// <param name="Status">The current state (e.g., Converged, Iterating).</param>
/// <param name="Artifact">The produced artifact, if any.</param>
/// <param name="Reflection">The meta-analysis of the cycle.</param>
public record CycleResult(
    string Status,
    Artifact? Artifact,
    Reflection? Reflection
);