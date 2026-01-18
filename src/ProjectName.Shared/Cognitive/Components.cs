namespace ProjectName.Shared.Cognitive;

// --- IDENTITY ---
public record Persona(string Role, string Voice, string Manifestation);

// --- STRATEGY ---
public abstract record ThinkingStrategy(string Name, string Instruction);

public record ChainOfThought()
    : ThinkingStrategy("Chain of Thought", "Think step-by-step linearly. Do not skip steps.");

public record TreeOfThought(int Branches)
    : ThinkingStrategy("Tree of Thought", $"Explore {Branches} distinct alternative paths before converging on a solution.");

public record GraphOfThought()
    : ThinkingStrategy("Graph of Thought", "Connect related concepts in a network. Identify dependencies before concluding.");

// --- LEARNING ---
public record Example<T>(T Input, string ExpectedOutput, string Explanation);

// --- BOUNDARIES ---
public class Constraints
{
    public List<string> MustDo { get; set; } = new();
    public List<string> MustNotDo { get; set; } = new();
    public string OutputFormat { get; set; } = "JSON";
}

// --- CONTEXT ---
public class CognitiveContext : Dictionary<string, object>
{
    public void AddSystemState(string key, object value) => this[key] = value;
}
