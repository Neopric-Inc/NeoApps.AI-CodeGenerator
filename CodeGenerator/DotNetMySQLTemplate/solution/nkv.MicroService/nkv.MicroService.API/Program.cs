using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
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
