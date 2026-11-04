using System;
using System.IO;
using System.Linq;
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
            // --install-browsers [chromium|firefox|webkit] [--with-deps]: install Playwright's browsers and exit. Used by the
            // Dockerfile when custom-api/packages.json asks for a browser; runs before the host is built, so no MySQL,
            // Redis or RabbitMQ is needed at image build time.
            if (Array.IndexOf(args, "--install-browsers") is var bi && bi >= 0)
                return InstallBrowsers(args.Skip(bi + 1).ToArray());

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

        /// <summary>Runs Playwright's installer (Microsoft.Playwright, added via custom-api/packages.json) without a compile-time reference.</summary>
        private static int InstallBrowsers(string[] rest)
        {
            var playwright = Type.GetType("Microsoft.Playwright.Program, Microsoft.Playwright");
            if (playwright == null)
            {
                Console.Error.WriteLine("Microsoft.Playwright isn't referenced. Add it to workspace/<App>/custom-api/packages.json.");
                return 1;
            }
            var browsers = rest.Where(a => !a.StartsWith("-")).DefaultIfEmpty("chromium");
            var install = new[] { "install" }.Concat(rest.Where(a => a == "--with-deps")).Concat(browsers).ToArray();
            Console.WriteLine("playwright " + string.Join(" ", install));
            return (int)playwright.GetMethod("Main", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { install })!;
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
