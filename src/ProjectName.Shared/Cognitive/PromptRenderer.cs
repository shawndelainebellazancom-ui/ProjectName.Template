using System.Globalization; // FIX: Required for CultureInfo
using System.Text;
using System.Text.Json;

namespace ProjectName.Shared.Cognitive;

public static class PromptRenderer
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public static string Render<T>(CognitiveFrame<T> frame)
    {
        var sb = new StringBuilder();

        // 1. Render Persona (Identity)
        sb.AppendLine("@persona {");
        // FIX: CA1305 - Explicit InvariantCulture for interpolation
        sb.AppendLine(CultureInfo.InvariantCulture, $"  role: '{frame.Persona.Role}'");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  voice: '{frame.Persona.Voice}'");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  manifestation: '{frame.Persona.Manifestation}'");
        sb.AppendLine("}");

        // 2. Render Strategy (Thought Process)
        sb.AppendLine(CultureInfo.InvariantCulture, $"@thought_model {{ {frame.ThoughtProcess.Name}: {frame.ThoughtProcess.Instruction} }}");

        // 3. Render Learning (Few-Shot)
        // FIX: CA1860 - Use Count > 0 instead of Any()
        if (frame.LearningHistory.Count > 0)
        {
            sb.AppendLine("@examples {");
            foreach (var ex in frame.LearningHistory)
            {
                // We serialize the Input logic to keep it readable
                sb.AppendLine(CultureInfo.InvariantCulture, $"  input: {JsonSerializer.Serialize(ex.Input)}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  output: {ex.ExpectedOutput}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  insight: {ex.Explanation}");
            }
            sb.AppendLine("}");
        }

        // 4. Render Context
        sb.AppendLine("@context {");
        foreach (var kvp in frame.Context)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {kvp.Key}: {kvp.Value}");
        }
        sb.AppendLine("}");

        // 5. Render Constraints
        sb.AppendLine("@constraints {");
        foreach (var rule in frame.Constraints.MustDo) sb.AppendLine(CultureInfo.InvariantCulture, $"  DO: {rule}");
        foreach (var rule in frame.Constraints.MustNotDo) sb.AppendLine(CultureInfo.InvariantCulture, $"  NO: {rule}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  FORMAT: {frame.Constraints.OutputFormat}");
        sb.AppendLine("}");

        // 6. Render The Intent (The Payload)
        sb.AppendLine("@intent {");
        sb.AppendLine(JsonSerializer.Serialize(frame.Intent, _jsonOptions));
        sb.AppendLine("}");

        return sb.ToString();
    }
}
