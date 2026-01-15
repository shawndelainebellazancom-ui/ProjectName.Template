using ProjectName.McpServer.Tools;
using ProjectName.PlannerService.Grpc;
using ProjectName.MakerService.Grpc;
using ProjectName.CheckerService.Grpc;
using ProjectName.ReflectorService.Grpc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging (STDIO Safe - Write to Stderr to avoid breaking MCP JSON-RPC)
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Information);

// 2. gRPC Clients
builder.Services.AddGrpcClient<Planner.PlannerClient>(o => o.Address = new Uri("https+http://planner-service"));
builder.Services.AddGrpcClient<Maker.MakerClient>(o => o.Address = new Uri("https+http://maker-service"));
builder.Services.AddGrpcClient<Checker.CheckerClient>(o => o.Address = new Uri("https+http://checker-service"));
builder.Services.AddGrpcClient<Reflector.ReflectorClient>(o => o.Address = new Uri("https+http://reflector-service"));

// 3. MCP Server
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

// No MapDefaultEndpoints here if using Stdio to prevent noise
app.Run();