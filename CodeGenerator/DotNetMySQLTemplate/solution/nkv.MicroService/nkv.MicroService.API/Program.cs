using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using nkv.MicroService.Utility;
using NeoApps.Migrations;

namespace nkv.MicroService.API
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            var config = host.Services.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("MySQLDatabase");

            // --migrate / --baseline / --migrations-status / --migrate-down=VERSION: run that and exit.
            if (MigrationHost.RunFromArgs(args, connectionString, typeof(Program).Assembly) is int exitCode)
                return exitCode;

            // Apply pending migrations (Migrations/*.cs) before serving traffic, so a deploy moves the database
            // forward in place. If a migration fails the API does not start and the previous version keeps running.
            if (config.GetValue("Migrations:RunOnStartup", true))
                MigrationHost.Run(connectionString, typeof(Program).Assembly, "up");

            // Permission checks and data access are synchronous (Redis/MySQL): start with enough pool threads that a burst of
            // requests doesn't wait for the thread pool to grow (Redis timeouts). Api:MinThreads, default 64.
            var minThreads = config.GetValue("Api:MinThreads", 64);
            System.Threading.ThreadPool.GetMinThreads(out var workers, out var io);
            System.Threading.ThreadPool.SetMinThreads(Math.Max(workers, minThreads), Math.Max(io, minThreads));

            host.Run();
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    var assembly = Assembly.GetAssembly(typeof(Program));
                    var pathToConfig = Path.Combine(hostingContext.HostingEnvironment.ContentRootPath, "log4net.config");
                    _ = new AppLogManager(pathToConfig, assembly);
                    logging.AddLog4Net(new Log4NetProviderOptions { ExternalConfigurationSetup = true });
                })
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
