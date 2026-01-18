using Microsoft.AspNetCore.Mvc;
using ProjectName.ReflectorService.Grpc;
using ProjectName.Shared.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace ProjectName.OrchestrationApi.Controllers;

/// <summary>
/// Gateway controller for the Reflector Service.
/// </summary>
[ApiController]
[Route("reflector")]
[Produces("application/json")]
[SwaggerTag("Meta-cognitive analysis - determines convergence and provides improvement insights")]
public partial class ReflectorController(
    Reflector.ReflectorClient client,
    ILogger<ReflectorController> logger) : ControllerBase
{
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "REFLECTOR GATEWAY: Analyzing validation results")]
    private partial void LogReflectRequest();

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Error during reflection analysis")]
    private partial void LogReflectionError(Exception ex);

    /// <summary>
    /// Analyzes validation results for meta-cognition (Direct Access).
    /// </summary>
    /// <remarks>
    /// The Reflector performs meta-cognitive analysis to determine convergence (has the cycle achieved its goal),
    /// iteration guidance (what should be refined for the next cycle), quality insights (pattern recognition),
    /// and optimization suggestions (how to improve the intent or approach).
    /// The reflector is the thinking about thinking component that decides whether to converge or iterate.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "isValid": false,
    ///   "issues": [
    ///     "Missing error handling",
    ///     "No input validation"
    ///   ],
    ///   "confidenceScore": 65.0
    /// }
    /// ```
    /// 
    /// **Example Response (Converged):**
    /// ```json
    /// {
    ///   "insight": "Artifact meets all quality standards. Ready for production.",
    ///   "optimizedIntent": ""
    /// }
    /// ```
    /// 
    /// **Example Response (Iterating):**
    /// ```json
    /// {
    ///   "insight": "Validation identified critical gaps in error handling and input validation.",
    ///   "optimizedIntent": "Create a Python function with comprehensive error handling and input validation"
    /// }
    /// ```
    /// </remarks>
    /// <param name="validation">The validation report to analyze.</param>
    /// <returns>Reflection insights and convergence decision.</returns>
    /// <response code="200">Analysis completed successfully.</response>
    /// <response code="400">Invalid validation data.</response>
    /// <response code="500">Reflection service error.</response>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Analyze Validation Results",
        Description = "Performs meta-cognitive analysis to determine convergence or iteration needs",
        OperationId = "ReflectOnValidation",
        Tags = new[] { "Meta-Cognition" }
    )]
    [SwaggerResponse(200, "Analysis completed", typeof(Reflection))]
    [SwaggerResponse(400, "Invalid validation data")]
    [SwaggerResponse(500, "Reflection error")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public async Task<ActionResult<Reflection>> Reflect(
        [FromBody, SwaggerRequestBody("The validation results to analyze", Required = true)] Validation validation)
    {
        if (validation == null)
        {
            return BadRequest(new { error = "Validation data cannot be null" });
        }

        LogReflectRequest();

        try
        {
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
        catch (Exception ex)
        {
            LogReflectionError(ex);
            return StatusCode(500, new { error = "Reflection failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes historical cycle patterns for learning.
    /// </summary>
    /// <remarks>
    /// Performs trend analysis across multiple cycles to identify common failure patterns,
    /// success indicators, optimization opportunities, and learning trajectories.
    /// This endpoint enables the system to learn from past iterations and improve future planning strategies.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "cycles": [
    ///     {
    ///       "intentId": "intent-1",
    ///       "wasValid": false,
    ///       "confidenceScore": 55.0,
    ///       "timestamp": "2026-01-17T10:00:00Z"
    ///     },
    ///     {
    ///       "intentId": "intent-1-refined",
    ///       "wasValid": true,
    ///       "confidenceScore": 92.0,
    ///       "timestamp": "2026-01-17T10:05:00Z"
    ///     }
    ///   ],
    ///   "analysisType": "trend"
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "summary": {
    ///     "totalCycles": 2,
    ///     "successRate": 50.0,
    ///     "averageConfidence": 73.5,
    ///     "improvementTrend": 37.0
    ///   },
    ///   "insights": [
    ///     "Refinement iterations needed",
    ///     "Positive learning trajectory",
    ///     "Quality improvements recommended"
    ///   ],
    ///   "recommendation": "Consider refining prompts or adding validation rules."
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Historical cycle data to analyze.</param>
    /// <returns>Pattern analysis and insights.</returns>
    /// <response code="200">Historical analysis completed.</response>
    /// <response code="400">Invalid or insufficient cycle data.</response>
    [HttpPost("analyze-history")]
    [SwaggerOperation(
        Summary = "Analyze Historical Cycles",
        Description = "Performs trend analysis across multiple cycles for pattern recognition and learning",
        OperationId = "AnalyzeHistory",
        Tags = new[] { "Meta-Cognition" }
    )]
    [SwaggerResponse(200, "Historical analysis completed")]
    [SwaggerResponse(400, "Invalid cycle data")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public ActionResult<object> AnalyzeHistory(
        [FromBody, SwaggerRequestBody("Historical cycle data", Required = true)] HistoricalAnalysisRequest request)
    {
        if (request?.Cycles == null || request.Cycles.Count < 2)
        {
            return BadRequest(new { error = "At least 2 cycles required for historical analysis" });
        }

        // Calculate metrics
        var successRate = (double)request.Cycles.Count(c => c.WasValid) / request.Cycles.Count * 100;
        var averageConfidence = request.Cycles.Average(c => c.ConfidenceScore);
        var improvementTrend = request.Cycles.Count > 1
            ? request.Cycles.Last().ConfidenceScore - request.Cycles.First().ConfidenceScore
            : 0;

        return Ok(new
        {
            summary = new
            {
                totalCycles = request.Cycles.Count,
                successRate = Math.Round(successRate, 2),
                averageConfidence = Math.Round(averageConfidence, 2),
                improvementTrend = Math.Round(improvementTrend, 2)
            },
            insights = new[]
            {
                successRate > 75 ? "Strong convergence pattern detected" : "Refinement iterations needed",
                improvementTrend > 0 ? "Positive learning trajectory" : "Consider alternative approaches",
                averageConfidence > 80 ? "High confidence in outputs" : "Quality improvements recommended"
            },
            recommendation = successRate > 75
                ? "System is performing well. Continue with current strategy."
                : "Consider refining prompts or adding validation rules."
        });
    }

    /// <summary>
    /// Get reflector service health status.
    /// </summary>
    /// <remarks>
    /// Returns the operational status of the Reflector service.
    /// 
    /// **Example Response:**
    /// ```json
    /// {
    ///   "status": "healthy",
    ///   "service": "reflector",
    ///   "timestamp": "2026-01-17T10:30:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <returns>Service health information.</returns>
    /// <response code="200">Service is operational.</response>
    [HttpGet("health")]
    [SwaggerOperation(
        Summary = "Reflector Health Check",
        Description = "Verify the Reflector service is accessible and operational",
        OperationId = "ReflectorHealth",
        Tags = new[] { "Diagnostics" }
    )]
    [SwaggerResponse(200, "Service is healthy")]
    [Produces("application/json")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "reflector",
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Request model for historical cycle analysis.
/// </summary>
[SwaggerSchema(Description = "Container for historical cycle data used in trend analysis")]
public class HistoricalAnalysisRequest
{
    /// <summary>
    /// List of cycle records to analyze.
    /// </summary>
    [SwaggerSchema("Historical cycle data for pattern analysis")]
    public List<CycleRecord> Cycles { get; set; } = [];

    /// <summary>
    /// Type of analysis to perform (trend, pattern, optimization).
    /// </summary>
    [SwaggerSchema("Analysis type: trend, pattern, or optimization")]
    public string AnalysisType { get; set; } = "trend";
}

/// <summary>
/// Individual cycle record for historical analysis.
/// </summary>
[SwaggerSchema(Description = "Single cycle execution record")]
public class CycleRecord
{
    /// <summary>
    /// Intent identifier for this cycle.
    /// </summary>
    [SwaggerSchema("Unique identifier for the intent")]
    public string IntentId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the cycle produced a valid artifact.
    /// </summary>
    [SwaggerSchema("Validation result: true if artifact was valid")]
    public bool WasValid { get; set; }

    /// <summary>
    /// Confidence score from validation (0-100).
    /// </summary>
    [SwaggerSchema("Validation confidence score (0-100)")]
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// When this cycle was executed.
    /// </summary>
    [SwaggerSchema("Cycle execution timestamp")]
    public DateTime Timestamp { get; set; }
}
