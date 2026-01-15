using Microsoft.Extensions.AI;
using Microsoft.OpenApi;
using OllamaSharp;
using ProjectName.OrchestrationApi.Clients;
using ProjectName.OrchestrationApi.Services;
using ProjectName.PlannerService.Grpc;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// --- 1. THE BRAIN (IChatClient) ---
if (builder.Environment.IsDevelopment())
{
    // FIX: Using OllamaApiClient which implements IChatClient natively
    builder.Services.AddChatClient(new OllamaApiClient(
        new Uri("http://localhost:11434"),
        "qwen2.5-coder"));
}

// --- 2. HTTP CLIENTS ---
builder.Services.AddHttpClient<MakerClient>(c => c.BaseAddress = new("https+http://maker-service"));
builder.Services.AddHttpClient<CheckerClient>(c => c.BaseAddress = new("https+http://checker-service"));
builder.Services.AddHttpClient<ReflectorClient>(c => c.BaseAddress = new("https+http://reflector-service"));

// --- 3. gRPC CLIENTS ---
builder.Services.AddGrpcClient<Planner.PlannerClient>(o => o.Address = new Uri("https+http://planner-service"));

// --- 4. THE NERVOUS SYSTEM ---
builder.Services.AddScoped<OrchestrationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PMCR-O Orchestrator", Version = "v1" });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    c.EnableAnnotations();
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();