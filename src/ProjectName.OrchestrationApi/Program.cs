using Microsoft.Extensions.AI;
using Microsoft.OpenApi;
using OllamaSharp;
using ProjectName.CheckerService.Grpc;
using ProjectName.MakerService.Grpc;
using ProjectName.OrchestrationApi.Clients;
using ProjectName.OrchestrationApi.Services;
using ProjectName.PlannerService.Grpc;
using ProjectName.ReflectorService.Grpc;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// --- 1. THE BRAIN (IChatClient) ---
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddChatClient(new OllamaApiClient(
        new Uri("http://localhost:11434"),
        "qwen2.5-coder"));
}

// --- 2. HTTP CLIENTS (For OrchestrationService / Agents) ---
builder.Services.AddHttpClient<ProjectName.OrchestrationApi.Clients.MakerClient>(c => c.BaseAddress = new("https://maker-service"));
builder.Services.AddHttpClient<ProjectName.OrchestrationApi.Clients.CheckerClient>(c => c.BaseAddress = new("https://checker-service"));
builder.Services.AddHttpClient<ProjectName.OrchestrationApi.Clients.ReflectorClient>(c => c.BaseAddress = new("https://reflector-service"));

// --- 3. gRPC CLIENTS (For Controllers / Direct Access) ---
builder.Services.AddGrpcClient<Planner.PlannerClient>(o => o.Address = new Uri("https://planner-service"));
builder.Services.AddGrpcClient<Maker.MakerClient>(o => o.Address = new Uri("https://maker-service"));
builder.Services.AddGrpcClient<Checker.CheckerClient>(o => o.Address = new Uri("https://checker-service"));
builder.Services.AddGrpcClient<Reflector.ReflectorClient>(o => o.Address = new Uri("https://reflector-service"));

// --- 4. THE NERVOUS SYSTEM ---
builder.Services.AddScoped<OrchestrationService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 5. ENHANCED SWAGGER CONFIGURATION ---
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PMCR-O Orchestration API",
        Version = "v1",
        Description = "# PMCR-O: Plan-Make-Check-Reflect Orchestrator\n\n" +
                      "A cognitive architecture for autonomous agent systems implementing a continuous improvement loop.\n\n" +
                      "## The PMCR-O Cycle\n\n" +
                      "* **Plan (P)**: Strategic decomposition of intents into actionable steps\n" +
                      "* **Make (M)**: Materialization of plans into concrete artifacts\n" +
                      "* **Check (C)**: Validation of artifacts against quality constraints\n" +
                      "* **Reflect (R)**: Meta-cognitive analysis for convergence determination\n\n" +
                      "## Architecture\n\n" +
                      "This API orchestrates communication between four core microservices via gRPC:\n\n" +
                      "* **PlannerService**: Intent → Execution Plan\n" +
                      "* **MakerService**: Plan → Artifact\n" +
                      "* **CheckerService**: Artifact → Validation Report\n" +
                      "* **ReflectorService**: Validation → Convergence Decision\n\n" +
                      "## Getting Started\n\n" +
                      "Use `/orchestrate/run-cycle` for the full PMCR loop, or access individual services directly via their gateway endpoints.\n\n" +
                      "**Quick Start**: POST to `/orchestrate/run-cycle` with an intent to see the complete cycle in action.",
        Contact = new OpenApiContact
        {
            Name = "PMCR-O System",
            Email = "support@example.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // XML Comments for detailed documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Enable Swagger Annotations
    options.EnableAnnotations();

    // Add operation filters for better documentation
    options.CustomSchemaIds(type => type.FullName);

    // Security (if needed in future)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// --- 6. SWAGGER UI CONFIGURATION ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PMCR-O API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
        options.DocumentTitle = "PMCR-O Orchestration API";
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Example);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
