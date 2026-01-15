using Microsoft.Extensions.AI;
using OllamaSharp;
using ProjectName.PlannerService.Services;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// FIX: CultureInfo.InvariantCulture
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// AI Client
var ollamaUrl = builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434";
var model = builder.Configuration["Ollama:Model"] ?? "qwen2.5-coder";

builder.Services.AddSingleton<IChatClient>(sp =>
    new OllamaApiClient(new Uri(ollamaUrl), model));

builder.Services.AddGrpc();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGrpcService<PlannerService>();
app.MapGet("/", () => "Planner Service Active (gRPC)");

app.Run();