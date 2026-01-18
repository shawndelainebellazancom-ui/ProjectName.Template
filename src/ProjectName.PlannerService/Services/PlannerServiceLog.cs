using Microsoft.Extensions.Logging;

namespace ProjectName.PlannerService.Services;

/// <summary>
/// High-performance logging for PlannerService using source-generated LoggerMessage delegates.
/// </summary>
internal static partial class PlannerServiceLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "📋 PLANNER: CreatePlan called - Intent: {Intent}")]
    public static partial void PlanRequest(ILogger logger, string intent);

    [LoggerMessage(Level = LogLevel.Information, Message = "✅ Plan {PlanId} created with {StepCount} steps")]
    public static partial void PlanCreated(ILogger logger, string planId, int stepCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "❌ PLANNER: Error creating plan")]
    public static partial void PlanError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "🔍 PLANNER: GatherIntel - Query: {Query}")]
    public static partial void ResearchRequest(ILogger logger, string query);

    [LoggerMessage(Level = LogLevel.Error, Message = "❌ PLANNER: Research failed")]
    public static partial void ResearchError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "✅ PLANNER: ValidateOutcome - Intent: {Intent}")]
    public static partial void ValidationRequest(ILogger logger, string intent);
}