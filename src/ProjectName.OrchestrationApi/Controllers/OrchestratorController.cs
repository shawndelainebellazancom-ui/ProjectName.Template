using Microsoft.AspNetCore.Mvc;
using ProjectName.PlannerService.Grpc;
using ProjectName.MakerService.Grpc;
using ProjectName.CheckerService.Grpc;
using ProjectName.ReflectorService.Grpc;
using ProjectName.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// The central nervous system of the agent. Orchestrates the flow of data between
/// Planner, Maker, Checker, and Reflector services via gRPC.
/// </summary>
[ApiController]
[Route("orchestrate")]
[Produces("application/json")]
[SwaggerTag("Core orchestration endpoint that executes the full PMCR-O cognitive cycle")]
public partial class OrchestratorController(
    Planner.PlannerClient planner,
    Maker.MakerClient maker,
    Checker.CheckerClient checker,
    Reflector.ReflectorClient reflector,
    ILogger<OrchestratorController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "ORCHESTRATOR: Received Intent: {Id}")]
    private partial void LogReceivedIntent(string id);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Error executing cycle for intent {IntentId}")]
    private partial void LogCycleExecutionError(Exception ex, string intentId);

    /// <summary>
    /// Executes a full cognitive cycle based on a seed intent.
    /// </summary>
    /// <remarks>
    /// This endpoint implements the complete PMCR-O loop:
    /// 
    /// 1. **Plan (P)**: Converts abstract intent into structured execution strategy
    /// 2. **Make (M)**: Materializes the plan into a concrete artifact
    /// 3. **Check (C)**: Validates the artifact against quality constraints
    /// 4. **Reflect (R)**: Analyzes results to determine convergence or iteration
    /// 
    /// The cycle returns either "Converged" (task complete) or "Iterating" (requires refinement).
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "id": "intent-001",
    ///   "content": "Create a Python function to calculate Fibonacci numbers",
    ///   "context": {
    ///     "environment": "production",
    ///     "language": "python"
    ///   }
    /// }
    /// ```
    /// 
    /// **Example Response (Converged):**
    /// ```json
    /// {
    ///   "status": "Converged",
    ///   "artifact": {
    ///     "id": "artifact-xyz",
    ///     "planId": "plan-abc",
    ///     "content": "def fibonacci(n):\n    if n &lt;= 1: return n\n    a, b = 0, 1\n    for _ in range(2, n + 1):\n        a, b = b, a + b\n    return b",
    ///     "artifactType": "Code/Python"
    ///   },
    ///   "reflection": {
    ///     "insight": "Artifact meets all quality standards.",
    ///     "optimizedIntent": ""
    ///   }
    /// }
    /// ```
    /// 
    /// **Example Response (Iterating):**
    /// ```json
    /// {
    ///   "status": "Iterating",
    ///   "artifact": {
    ///     "id": "artifact-xyz",
    ///     "planId": "plan-abc",
    ///     "content": "def fib(n): return fib(n-1)+fib(n-2) if n&gt;1 else n",
    ///     "artifactType": "Code/Python"
    ///   },
    ///   "reflection": {
    ///     "insight": "Exponential complexity detected. Needs optimization.",
    ///     "optimizedIntent": "Create an efficient iterative Fibonacci function"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="intent">The input intent containing the goal and context.</param>
    /// <returns>The result of the cycle, indicating convergence or iteration.</returns>
    /// <response code="200">Cycle completed successfully. Check the Status field for convergence.</response>
    /// <response code="400">Invalid intent provided (empty content, malformed JSON).</response>
    /// <response code="500">Internal error during cycle execution.</response>
    [HttpPost("run-cycle")]
    [SwaggerOperation(
        Summary = "Execute Full PMCR-O Cycle",
        Description = "Runs the complete Plan-Make-Check-Reflect loop for a given intent",
        OperationId = "ExecuteCycle",
        Tags = new[] { "Orchestration" }
    )]
    [SwaggerResponse(200, "Cycle completed successfully", typeof(CycleResult))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(500, "Internal server error")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<ActionResult<CycleResult>> ExecuteCycle(
        [FromBody, SwaggerRequestBody("The seed intent to process", Required = true)] Intent intent)
    {
        if (intent == null || string.IsNullOrWhiteSpace(intent.Content))
        {
            return BadRequest(new { error = "Intent content cannot be empty" });
        }

        LogReceivedIntent(intent.Id);

        try
        {
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
            var makeRequest = new MakeRequest { PlanId = domainPlan.Id };
            makeRequest.Steps.AddRange(domainPlan.Steps);
            foreach (var r in domainPlan.Resources) makeRequest.Resources.Add(r.Key, r.Value);

            // --- INTELLIGENT ROUTING FIX ---
            // Extract target language from Context or Resources to guide the Maker.
            if (intent.Context.TryGetValue("language", out var lang) ||
                domainPlan.Resources.TryGetValue("language", out lang))
            {
                makeRequest.ArtifactType = lang;
            }
            else
            {
                // Default to standard code if not specified
                makeRequest.ArtifactType = "Code";
            }
            // -------------------------------

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
        catch (Exception ex)
        {
            LogCycleExecutionError(ex, intent.Id);
            return StatusCode(500, new { error = "Cycle execution failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for the orchestrator.
    /// </summary>
    /// <remarks>
    /// Returns the operational status of the Orchestrator service.
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "status": "healthy",
    ///   "service": "orchestrator",
    ///   "timestamp": "2026-01-17T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <returns>Service health status.</returns>
    /// <response code="200">Service is healthy and operational.</response>
    [HttpGet("health")]
    [SwaggerOperation(
        Summary = "Health Check",
        Description = "Verify orchestrator service is running",
        OperationId = "HealthCheck",
        Tags = new[] { "Diagnostics" }
    )]
    [SwaggerResponse(200, "Service is healthy")]
    [Produces("application/json")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "orchestrator",
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Encapsulates the outcome of a cognitive cycle.
/// </summary>
/// <param name="Status">The current state: Converged (complete) or Iterating (refining).</param>
/// <param name="Artifact">The produced artifact, if any.</param>
/// <param name="Reflection">The meta-analysis of the cycle.</param>
[SwaggerSchema(Description = "Result of a complete PMCR-O cognitive cycle")]
public record CycleResult(
    [property: SwaggerSchema("Convergence status: 'Converged' or 'Iterating'")] string Status,
    [property: SwaggerSchema("The artifact produced by the Maker service")] Artifact? Artifact,
    [property: SwaggerSchema("Meta-cognitive analysis from the Reflector")] Reflection? Reflection
);
