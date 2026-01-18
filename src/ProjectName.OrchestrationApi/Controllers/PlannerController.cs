using Microsoft.AspNetCore.Mvc;
using ProjectName.PlannerService.Grpc;
using ProjectName.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Planner Service.
/// </summary>
[ApiController]
[Route("planner")]
[Produces("application/json")]
[SwaggerTag("Strategic planning - decomposes intents into actionable execution plans")]
public partial class PlannerController(
    Planner.PlannerClient client,
    ILogger<PlannerController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "PLANNER GATEWAY: Requesting plan for Intent {Id}")]
    private partial void LogPlanningRequest(string id);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error creating plan for intent {IntentId}")]
    private partial void LogPlanningError(Exception ex, string intentId);

    /// <summary>
    /// Decomposes an intent into a structured plan (Direct Access).
    /// </summary>
    /// <remarks>
    /// The Planner analyzes the intent and breaks it down into sequential execution steps,
    /// required resources (runtime, tools, dependencies), and detailed action plan with tool assignments.
    /// This is the P (Plan) phase of the PMCR-O cycle.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "id": "intent-123",
    ///   "content": "Deploy a containerized web service",
    ///   "context": {
    ///     "platform": "kubernetes",
    ///     "environment": "staging"
    ///   }
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "id": "plan-456",
    ///   "originalIntentId": "intent-123",
    ///   "steps": [
    ///     "Create Dockerfile",
    ///     "Build container image",
    ///     "Push to registry",
    ///     "Deploy to Kubernetes"
    ///   ],
    ///   "resources": {
    ///     "runtime": "docker",
    ///     "orchestrator": "kubernetes",
    ///     "environment": "staging"
    ///   }
    /// }
    /// ```
    /// </remarks>
    /// <param name="intent">The seed intent to plan.</param>
    /// <returns>A structured execution plan with steps and resources.</returns>
    /// <response code="200">Plan created successfully.</response>
    /// <response code="400">Invalid intent (empty content).</response>
    /// <response code="500">Planning service error.</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create Execution Plan",
        Description = "Converts an abstract intent into a structured, actionable plan",
        OperationId = "CreatePlan",
        Tags = new[] { "Planning" }
    )]
    [SwaggerResponse(200, "Plan created successfully", typeof(Plan))]
    [SwaggerResponse(400, "Invalid intent")]
    [SwaggerResponse(500, "Planning error")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<ActionResult<Plan>> CreatePlan(
        [FromBody, SwaggerRequestBody("The intent to decompose into a plan", Required = true)] Intent intent)
    {
        if (intent == null || string.IsNullOrWhiteSpace(intent.Content))
        {
            return BadRequest(new { error = "Intent content cannot be empty" });
        }

        LogPlanningRequest(intent.Id);

        try
        {
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
        catch (Exception ex)
        {
            LogPlanningError(ex, intent.Id);
            return StatusCode(500, new { error = "Planning failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Gather intelligence for planning (Research capability).
    /// </summary>
    /// <remarks>
    /// The Planner can perform research before creating a plan.
    /// This uses the Eyes component to gather real-world information.
    /// Useful for finding current best practices, discovering available tools/libraries,
    /// researching domain-specific constraints, and validating feasibility.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "query": "Best practices for Kubernetes deployment in 2026",
    ///   "domains": ["devops", "kubernetes"],
    ///   "maxResults": 5
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "success": true,
    ///   "intel": "Found 5 relevant sources about K8s deployment patterns...",
    ///   "sources": [
    ///     {
    ///       "url": "https://kubernetes.io/docs/concepts/workloads/",
    ///       "title": "Workload Resources",
    ///       "snippet": "Overview of deployment strategies...",
    ///       "relevance": 0.95
    ///     }
    ///   ],
    ///   "errorMessage": null
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Research query parameters.</param>
    /// <returns>Intelligence report with sources.</returns>
    /// <response code="200">Research completed successfully.</response>
    /// <response code="400">Invalid query.</response>
    [HttpPost("research")]
    [SwaggerOperation(
        Summary = "Gather Intelligence",
        Description = "Performs research to gather information for better planning",
        OperationId = "GatherIntel",
        Tags = new[] { "Planning" }
    )]
    [SwaggerResponse(200, "Research completed")]
    [SwaggerResponse(400, "Invalid query")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<ActionResult<object>> GatherIntel(
        [FromBody, SwaggerRequestBody("Research query", Required = true)] ResearchQuery request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Query cannot be empty" });
        }

        var researchRequest = new ResearchRequest
        {
            Query = request.Query,
            MaxResults = request.MaxResults
        };
        researchRequest.Domains.AddRange(request.Domains);

        var reply = await client.GatherIntelAsync(researchRequest);

        return Ok(new
        {
            success = reply.Success,
            intel = reply.Intel,
            sources = reply.Sources.Select(s => new
            {
                s.Url,
                s.Title,
                s.Snippet,
                relevance = s.RelevanceScore
            }),
            errorMessage = reply.ErrorMessage
        });
    }

    /// <summary>
    /// Get planner service health status.
    /// </summary>
    /// <remarks>
    /// Returns the operational status of the Planner service.
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "status": "healthy",
    ///   "service": "planner",
    ///   "timestamp": "2026-01-17T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <returns>Service health information.</returns>
    /// <response code="200">Service is operational.</response>
    [HttpGet("health")]
    [SwaggerOperation(
        Summary = "Planner Health Check",
        Description = "Verify the Planner service is accessible and operational",
        OperationId = "PlannerHealth",
        Tags = new[] { "Diagnostics" }
    )]
    [SwaggerResponse(200, "Service is healthy")]
    [Produces("application/json")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "planner",
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Research query request model.
/// </summary>
[SwaggerSchema(Description = "Parameters for intelligence gathering research")]
public class ResearchQuery
{
    /// <summary>
    /// Search query string.
    /// </summary>
    [SwaggerSchema("The research question or search query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Target domains for research.
    /// </summary>
    [SwaggerSchema("Specific domains to search (e.g., 'devops', 'security')")]
    public List<string> Domains { get; set; } = [];

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    [SwaggerSchema("Maximum number of research results (default: 10)")]
    public int MaxResults { get; set; } = 10;
}
