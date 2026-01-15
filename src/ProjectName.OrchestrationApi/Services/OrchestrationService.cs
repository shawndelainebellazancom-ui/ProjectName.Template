using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ProjectName.PlannerService.Grpc;
using ProjectName.Shared.Models;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace ProjectName.OrchestrationApi.Services;

public partial class OrchestrationService
{
    private readonly Planner.PlannerClient _plannerClient;
    private readonly IChatClient _baseChatClient;
    private readonly ILogger<OrchestrationService> _logger;
    private readonly Clients.MakerClient _makerClient;
    private readonly Clients.CheckerClient _checkerClient;
    private readonly Clients.ReflectorClient _reflectorClient;

    public OrchestrationService(
        Planner.PlannerClient plannerClient,
        IChatClient chatClient,
        Clients.MakerClient makerClient,
        Clients.CheckerClient checkerClient,
        Clients.ReflectorClient reflectorClient,
        ILogger<OrchestrationService> logger)
    {
        _plannerClient = plannerClient;
        _baseChatClient = chatClient;
        _makerClient = makerClient;
        _checkerClient = checkerClient;
        _reflectorClient = reflectorClient;
        _logger = logger;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "🧠 Orchestration: Starting PMCR-O cycle for intent: {Intent}")]
    private partial void LogStartCycle(string intent);

    [LoggerMessage(Level = LogLevel.Information, Message = "✅ Orchestration: Cycle completed with status: {Status}")]
    private partial void LogCycleComplete(string status);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent {ExecutorId} output: {Content}")]
    private partial void LogAgentOutput(string executorId, string content);

    [LoggerMessage(Level = LogLevel.Error, Message = "Orchestration failed for intent: {Intent}")]
    private partial void LogOrchestrationFailure(Exception ex, string intent);

    // ------------------------------------------------------------
    // AGENT FACTORY HELPERS
    // ------------------------------------------------------------

    private ChatClientAgent CreateAgentWithTools(string name, string description, IList<AITool> tools)
    {
        var options = new ChatClientAgentOptions
        {
            Name = name,
            Description = description,
            ChatOptions = new ChatOptions
            {
                Tools = tools
            }
        };

        return new ChatClientAgent(_baseChatClient, options);
    }

    private ChatClientAgent CreatePlannerAgent()
    {
        var tools = new List<AITool>
        {
        AIFunctionFactory.Create(
            CallPlannerServiceAsync,
            "create_plan",
            "Creates a plan from intent"
        )
        };

        return CreateAgentWithTools("PlannerAgent", "Strategic planner that decomposes intents", tools);
    }

    private ChatClientAgent CreateMakerAgent()
    {
        var tools = new List<AITool>
        {
AIFunctionFactory.Create(
    CallPlannerServiceAsync,
    "create_plan",
    "Creates a plan from intent"
)
        };

        return CreateAgentWithTools("MakerAgent", "Executor that performs planned actions", tools);
    }

    private ChatClientAgent CreateCheckerAgent()
    {
        var tools = new List<AITool>
        {
AIFunctionFactory.Create(
    CallPlannerServiceAsync,
    "create_plan",
    "Creates a plan from intent"
)
        };

        return CreateAgentWithTools("CheckerAgent", "Validator that verifies results", tools);
    }

    private ChatClientAgent CreateReflectorAgent()
    {
        var tools = new List<AITool>
        {
AIFunctionFactory.Create(
    CallPlannerServiceAsync,
    "create_plan",
    "Creates a plan from intent"
)
        };

        return CreateAgentWithTools("ReflectorAgent", "Analyzer that identifies improvements", tools);
    }

    // ------------------------------------------------------------
    // EXECUTION PIPELINE
    // ------------------------------------------------------------

    public async Task<OrchestrationResult> ExecuteIntentAsync(Intent intent, CancellationToken ct = default)
    {
        LogStartCycle(intent.Content);

        try
        {
            var planner = CreatePlannerAgent();
            var maker = CreateMakerAgent();
            var checker = CreateCheckerAgent();
            var reflector = CreateReflectorAgent();

            var endNode = new ChatClientAgent(
                _baseChatClient,
                new ChatClientAgentOptions
                {
                    Name = "End",
                    Description = "Terminator"
                });

            var builder = new WorkflowBuilder(planner);

            var workflow = builder
                .AddEdge(planner, maker)
                .AddEdge(maker, checker)
                .AddEdge(checker, reflector)
                .AddSwitch(reflector, map => map
                    .AddCase<ChatMessage>(msg => (msg?.Text ?? "").Contains("ITERATE"), planner)
                    .WithDefault(endNode))
                .Build();

            var run = await InProcessExecution.RunAsync(workflow, intent.Content, cancellationToken: ct);

            var result = new OrchestrationResult
            {
                Status = "Success",
                Output = new StringBuilder()
            };

            if (run?.NewEvents != null)
            {
                foreach (var evt in run.NewEvents)
                {
                    if (evt is AgentRunUpdateEvent agentEvent && agentEvent.Update != null)
                    {
                        var content = agentEvent.Update?.Text ?? string.Empty;
                        var agentId = agentEvent.ExecutorId ?? "Unknown";

                        result.Output.AppendLine(
                            CultureInfo.InvariantCulture,
                            $"[{agentId}]: {content}");

                        LogAgentOutput(agentId, content.Length > 100 ? content[..100] + "..." : content);
                    }
                }
            }

            LogCycleComplete("Success");
            return result;
        }
        catch (Exception ex)
        {
            LogOrchestrationFailure(ex, intent.Content);
            return new OrchestrationResult
            {
                Status = "Failed",
                ErrorMessage = ex.Message
            };
        }
    }

    // ------------------------------------------------------------
    // TOOL IMPLEMENTATIONS
    // ------------------------------------------------------------

    private async Task<string> CallPlannerServiceAsync(string intent)
    {
        var request = new IntentRequest { Id = Guid.NewGuid().ToString(), Content = intent };
        var reply = await _plannerClient.CreatePlanAsync(request);
        return JsonSerializer.Serialize(reply);
    }

    private async Task<string> CallMakerServiceAsync(string planJson)
    {
        try
        {
            var plan = JsonSerializer.Deserialize<Plan>(planJson);
            if (plan == null) return "Error: Invalid Plan JSON";

            var artifact = await _makerClient.ExecuteAsync(plan);
            return JsonSerializer.Serialize(artifact);
        }
        catch (Exception ex)
        {
            return $"Error making artifact: {ex.Message}";
        }
    }

    private async Task<string> CallCheckerServiceAsync(string artifactJson)
    {
        try
        {
            var artifact = JsonSerializer.Deserialize<Artifact>(artifactJson);
            if (artifact == null) return "Error: Invalid Artifact JSON";

            var validation = await _checkerClient.ValidateAsync(artifact);
            return JsonSerializer.Serialize(validation);
        }
        catch (Exception ex)
        {
            return $"Error checking artifact: {ex.Message}";
        }
    }

    private async Task<string> CallReflectorServiceAsync(string validationJson)
    {
        try
        {
            var validation = JsonSerializer.Deserialize<Validation>(validationJson);
            if (validation == null) return "Error: Invalid Validation JSON";

            var reflection = await _reflectorClient.AnalyzeAsync(validation);

            return string.IsNullOrEmpty(reflection?.OptimizedIntent)
                ? "CONVERGED"
                : $"ITERATE: {reflection.OptimizedIntent}";
        }
        catch (Exception ex)
        {
            return $"Error reflecting: {ex.Message}";
        }
    }
}

public class OrchestrationResult
{
    public string Status { get; set; } = "Unknown";
    public StringBuilder Output { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
