using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;
using ProjectName.PlannerService.Services;
using ProjectName.PlannerService.Tools;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Logging
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// --- 1. THE BRAIN ---
var ollamaUrl = builder.Configuration["Ollama:Endpoint"] ?? "http://host.docker.internal:11434";
var model = builder.Configuration["Ollama:Model"] ?? "qwen2.5-coder";

builder.Services.AddChatClient(new OllamaApiClient(new Uri(ollamaUrl), model));

// --- 2. THE PLANNER AGENT ---
builder.Services.AddSingleton<AIAgent>(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return chatClient.CreateAIAgent(
        name: "Planner",
        instructions: """
            IDENTITY: You are a Strategic Planner.
            TASK: Break down the user's intent into a sequential, actionable list of steps.
            OUTPUT FORMAT:
            - Step 1
            - Step 2
            - Step 3
            CONSTRAINT: Do not be chatty. Return ONLY the list of steps.
            """);
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<ResearchTool>();
builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<PlannerService>();
app.MapGet("/", () => "Planner Service (Intelligent) Active");

app.Run();
