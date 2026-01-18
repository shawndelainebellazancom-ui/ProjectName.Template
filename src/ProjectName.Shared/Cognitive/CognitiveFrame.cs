namespace ProjectName.Shared.Cognitive;

/// <summary>
/// The Cognitive Frame is the container for a single cycle of reasoning.
/// It wraps the raw Intent with Identity, Strategy, and Constraints.
/// </summary>
/// <typeparam name="TIntent">The specific type of data we are processing.</typeparam>
public class CognitiveFrame<TIntent>
{
    // 1. Identity Layer (Who am I?)
    public Persona Persona { get; set; } = new("Default", "Neutral", "Assistant");

    // 2. Data Layer (What am I working on?)
    // FIX: Added 'required' to satisfy CS8618
    public required TIntent Intent { get; set; }

    // 3. Context Layer (What is the environment?)
    // Holds dynamic state like "BrowserIsOpen", "FileExists", "PreviousErrors"
    public CognitiveContext Context { get; set; } = new();

    // 4. Strategy Layer (How should I think?)
    // Defines the algorithm: Chain of Thought, Tree of Thought, etc.
    public ThinkingStrategy ThoughtProcess { get; set; } = new ChainOfThought();

    // 5. Learning Layer (Few-Shot Examples)
    // Injects previous successful patterns to guide the model.
    public List<Example<TIntent>> LearningHistory { get; set; } = new();

    // 6. Constraints (The Safety Rails)
    public Constraints Constraints { get; set; } = new();
}
