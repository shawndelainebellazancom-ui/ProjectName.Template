using Microsoft.Extensions.DependencyInjection;
using ProjectName.McpServer.Domain;
using ProjectName.McpServer.Tools;
using ModelContextProtocol.Server;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Logging - Configured for Strict Mode (Invariant Culture)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// 1. REGISTER DOMAIN SERVICES
builder.Services.AddSingleton<CompilerService>();
builder.Services.AddSingleton<InspectorService>();
builder.Services.AddSingleton<BrowserService>();

// 2. MCP SERVER CONFIGURATION
// We enable both transports:
// - Stdio: For running via "npx" or Claude Desktop.
// - Http: For running via Docker, Postman, or HTTP calls (SSE).
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithHttpTransport() // <--- FIX: Correct method name
    .WithToolsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();

// 3. MAP THE SSE ENDPOINT
// This allows Postman to connect via "https://localhost:PORT/sse"
app.MapMcp("/sse"); // <--- FIX: Correct method name

app.MapGet("/", () => "Sovereign MCP Server Online (Transports: STDIO + SSE)");

app.Run();
