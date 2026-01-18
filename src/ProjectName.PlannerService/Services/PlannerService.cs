using Grpc.Core;
using Microsoft.Agents.AI;
using ProjectName.PlannerService.Grpc;
using ProjectName.PlannerService.Tools; // Needed for ResearchTool if used later
using System.Globalization;

namespace ProjectName.PlannerService.Services;

public class PlannerService(AIAgent plannerAgent, ILogger<PlannerService> logger) : Planner.PlannerBase
{
    // --- MEMORY OPTIMIZATION: Static Readonly Fields ---
    // Prevents allocating new arrays on every request.
    private static readonly char[] _lineSeparators = ['\n', '\r'];
    private static readonly char[] _bulletPointChars = ['-', '*', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ' '];

    public override async Task<PlanReply> CreatePlan(IntentRequest request, ServerCallContext context)
    {
        PlannerServiceLog.PlanRequest(logger, request.Content);

        try
        {
            // 1. ASK THE AI FOR STEPS
            // We include the Context in the prompt so the Planner knows the constraints (e.g. language=python).
            var prompt = string.Format(
                CultureInfo.InvariantCulture,
                "INTENT: {0}\nCONTEXT: {1}",
                request.Content,
                string.Join(", ", request.Context.Select(x => $"{x.Key}={x.Value}"))
            );

            var response = await plannerAgent.RunAsync(prompt, cancellationToken: context.CancellationToken);
            var aiText = response.ToString();

            // 2. PARSE THE STEPS
            // Optimization: Use the static readonly separators.
            var steps = aiText.Split(_lineSeparators, StringSplitOptions.RemoveEmptyEntries)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Select(s => s.Trim().TrimStart(_bulletPointChars)) // Clean common list markers
                              .ToList();

            // 3. BUILD RESPONSE
            var reply = new PlanReply
            {
                Id = Guid.NewGuid().ToString(),
                OriginalIntentId = request.Id,
                Goal = $"Achieve: {request.Content}",
                Analysis = "AI Generated Strategy"
            };

            reply.Steps.AddRange(steps);

            // Pass through context as resources so the Maker can use them
            foreach (var kvp in request.Context)
            {
                reply.Resources.Add(kvp.Key, kvp.Value);
            }

            PlannerServiceLog.PlanCreated(logger, reply.Id, steps.Count);
            return reply;
        }
        catch (Exception ex)
        {
            PlannerServiceLog.PlanError(logger, ex);
            return new PlanReply { Goal = "Error", Analysis = ex.Message };
        }
    }

    // --- STUBBED METHODS (Required for Interface Compliance) ---

    public override Task<ResearchResponse> GatherIntel(ResearchRequest request, ServerCallContext context)
    {
        // Future: Wire up ResearchTool here
        return Task.FromResult(new ResearchResponse { Success = true, Intel = "Research skipped for now." });
    }

    public override Task<ValidationResponse> ValidateOutcome(ValidationRequest request, ServerCallContext context)
    {
        return Task.FromResult(new ValidationResponse { Success = true, IsValid = true });
    }

    public override Task<HealthCheckResponse> HealthCheck(HealthCheckRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HealthCheckResponse { Status = HealthCheckResponse.Types.ServingStatus.Serving });
    }
}
