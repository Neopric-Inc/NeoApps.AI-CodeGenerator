using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoApps.Mcp.Services;

var builder = Host.CreateApplicationBuilder(args);

// stdout is the MCP channel: every log line must go to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
    .AddSingleton(NeoAppsSettings.FromEnvironment())
    .AddSingleton<JobManager>()
    .AddSingleton<ProjectStore>()
    .AddSingleton<GeneratorRunner>()
    .AddSingleton<DockerService>();

builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "neoapps", Version = "2.0.0" })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
