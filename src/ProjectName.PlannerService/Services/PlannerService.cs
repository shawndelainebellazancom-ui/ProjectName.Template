using Grpc.Core;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ProjectName.PlannerService.Grpc;

namespace ProjectName.PlannerService.Services;

public partial class PlannerService(
    IChatClient chatClient,
    ILogger<PlannerService> logger) : Planner.PlannerBase
{
    [LoggerMessage(Level = LogLevel.Information, Message = "PLANNER: Decomposing Intent {Id}")]
    private partial void LogPlanning(string id);

    public override async Task<PlanReply> CreatePlan(IntentRequest request, ServerCallContext context)
    {
        LogPlanning(request.Id);

        var messages = new List<ChatMessage>
    {
        new(ChatRole.System, "You are an expert system architect. Break the intent into clear, numbered steps."),
        new(ChatRole.User, $"Intent: {request.Content}")
    };

        // IChatClient → must use GetResponseAsync
        var response = await chatClient.GetResponseAsync(messages);

        // FIX: ChatResponse no longer has .Message
        var responseText =
            response.Text ?? string.Empty;

        // Parse steps from LLM output
        var parsedSteps = responseText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.TrimStart('-', '*', ' ', '\t'))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var reply = new PlanReply
        {
            Id = Guid.NewGuid().ToString(),
            OriginalIntentId = request.Id,
        };

        reply.Steps.AddRange(parsedSteps);
        reply.Resources.Add("LLM", "Ollama");

        return reply;
    }

}
