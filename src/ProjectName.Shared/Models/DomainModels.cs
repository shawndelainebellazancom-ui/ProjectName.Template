namespace ProjectName.Shared.Models;

/// <summary>
/// Represents the seed intent provided by the user or system.
/// </summary>
/// <param name="Id">Unique identifier for this intent.</param>
/// <param name="Content">The raw textual description of the goal.</param>
/// <param name="Context">Additional key-value metadata (e.g., environment, constraints).</param>
public record Intent(
    string Id,
    string Content,
    Dictionary<string, string> Context
);

/// <summary>
/// Represents the structured execution strategy derived from the Intent.
/// </summary>
/// <param name="Id">Unique identifier for this plan.</param>
/// <param name="OriginalIntentId">Link back to the seed intent.</param>
/// <param name="Steps">Ordered list of execution steps.</param>
/// <param name="Resources">Required resources identified for this plan.</param>
public record Plan(
    string Id,
    string OriginalIntentId,
    List<string> Steps,
    Dictionary<string, string> Resources
);

/// <summary>
/// Represents a materialized output created by the Maker.
/// </summary>
/// <param name="Id">Unique identifier for the artifact.</param>
/// <param name="PlanId">Link back to the plan that generated it.</param>
/// <param name="Content">The actual content (code, text, config).</param>
/// <param name="ArtifactType">The type of artifact (e.g., "CSharp", "Markdown").</param>
public record Artifact(
    string Id,
    string PlanId,
    string Content,
    string ArtifactType
);

/// <summary>
/// Represents the validation result from the Checker.
/// </summary>
/// <param name="IsValid">True if the artifact meets all criteria.</param>
/// <param name="Issues">List of detected problems or warnings.</param>
/// <param name="ConfidenceScore">A score (0-100) indicating validation confidence.</param>
public record Validation(
    bool IsValid,
    string[] Issues,
    double ConfidenceScore
);

/// <summary>
/// Represents the meta-cognitive analysis from the Reflector.
/// </summary>
/// <param name="Insight">Human-readable analysis of the cycle.</param>
/// <param name="OptimizedIntent">A refined intent for the next cycle (if iterating).</param>
public record Reflection(
    string Insight,
    string OptimizedIntent
);