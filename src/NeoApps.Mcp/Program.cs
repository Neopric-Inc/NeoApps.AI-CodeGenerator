using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NeoApps.Mcp.Services;

// CLI mode: `neoapps new TodoApp --schema todo.sql --deploy` etc. No arguments = MCP server on stdio.
if (args.Length > 0 && NeoApps.Mcp.Cli.IsCommand(args[0]))
    return await NeoApps.Mcp.Cli.RunAsync(args);

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
return 0;
