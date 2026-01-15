var builder = DistributedApplication.CreateBuilder(args);

// 1. Define Services
var planner = builder.AddProject<Projects.ProjectName_PlannerService>("planner-service");
var maker = builder.AddProject<Projects.ProjectName_MakerService>("maker-service");
var checker = builder.AddProject<Projects.ProjectName_CheckerService>("checker-service");
var reflector = builder.AddProject<Projects.ProjectName_ReflectorService>("reflector-service");

// 2. Define Orchestrator (API Gateway)
builder.AddProject<Projects.ProjectName_OrchestrationApi>("orchestration-api")
    .WithReference(planner)
    .WithReference(maker)
    .WithReference(checker)
    .WithReference(reflector);

// 3. Define MCP Server (Agent Interface)
builder.AddProject<Projects.ProjectName_McpServer>("mcp-server")
    .WithReference(planner)
    .WithReference(maker)
    .WithReference(checker)
    .WithReference(reflector);

builder.Build().Run();