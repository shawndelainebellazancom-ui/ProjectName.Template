using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OllamaSharp;
using ProjectName.CheckerService;
using ProjectName.CheckerService.Services;
using System.Globalization;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// --- 1. THE BRAIN ---
var ollamaUrl = builder.Configuration["Ollama:Endpoint"] ?? "http://host.docker.internal:11434";
var model = builder.Configuration["Ollama:Model"] ?? "qwen2.5-coder";
builder.Services.AddChatClient(new OllamaApiClient(new Uri(ollamaUrl), model));

// --- 2. THE CRITIC AGENT ---
builder.Services.AddSingleton<AIAgent>(sp =>
{
    var chatClient = sp.GetRequiredService<IChatClient>();
    return chatClient.CreateAIAgent(
        name: "Checker",
        instructions: """
            IDENTITY: You are a Senior Code Reviewer.
            TASK: Validate the artifact.
            INPUT: You will receive the Artifact Content AND a 'Forensic Report' from the compiler/tools.
            CRITICAL: If the Forensic Report says 'Build Failed', you MUST mark it as INVALID.
            OUTPUT FORMAT:
            - VALID or INVALID
            - List of issues.
            """);
});

// --- 3. THE HANDS (MCP CLIENT) ---
builder.Services.AddSingleton<McpClientFactory>(sp =>
{
    // Get the Factory that knows about Service Discovery
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    return async () =>
    {
        // Use the Aspire Resource Name (must match AppHost)
        var mcpUrl = "https://mcp-server/sse";

        // Create a SMART client that resolves service names
        var httpClient = httpClientFactory.CreateClient();

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(mcpUrl)
        }, httpClient, loggerFactory); // Pass the smart client here

        var client = await McpClient.CreateAsync(transport, new McpClientOptions(), loggerFactory: loggerFactory);

        return client;
    };
});

builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<ProjectName.CheckerService.Services.CheckerService>();
app.MapGet("/", () => "Checker Service (Intelligent + MCP Connected)");

app.Run();

namespace ProjectName.CheckerService
{
    public delegate Task<McpClient> McpClientFactory();
}
