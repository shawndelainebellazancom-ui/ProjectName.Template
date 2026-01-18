using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using ProjectName.MakerService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// --- 1. THE BRAIN (OLLAMA) ---
// Using host.docker.internal to reach the host machine's Ollama
var ollamaUrl = builder.Configuration["Ollama:Endpoint"] ?? "http://host.docker.internal:11434";
var modelName = builder.Configuration["Ollama:Model"] ?? "qwen2.5-coder";

// Register the raw Chat Client
builder.Services.AddChatClient(new OllamaApiClient(new Uri(ollamaUrl), modelName));

// --- 2. THE AGENT (THE MAKER) ---
// We wrap the raw client in a ChatClientAgent with specific instructions.
builder.Services.AddSingleton<AIAgent>(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();

    return chatClient.CreateAIAgent(
        name: "Maker",
        instructions: """
            IDENTITY: You are a high-performance software engineer.
            TASK: Translate the provided execution plan into production-ready code.
            CONSTRAINT: Return ONLY the code. Do not wrap in markdown blocks.
            CRITICAL: ALWAYS wrap methods in a public class. Never return standalone methods.
            """);
});

builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<MakerService>();
app.MapGet("/", () => "Maker Service (Agent Framework) is ONLINE.");

app.Run();
