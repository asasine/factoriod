using Factoriod.Fetcher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Factoriod.Daemon
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureLogging(builder =>
                {
                    builder.AddSimpleConsole(options =>
                    {
                        options.ColorBehavior = LoggerColorBehavior.Default;
                    });
                })
                .ConfigureHostConfiguration(config =>
                {
                    // despite CreateDefaultBuiler() being called, which should add appsettings.json,
                    // it has to be manually added here to grab the file bundled into the single-file executable
                    config.AddJsonFile("appsettings.json");
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<FactorioHostedService>();
                    services.AddHttpClient<VersionFetcher>();
                    services.AddHttpClient<ReleaseFetcher>();

                    services.Configure<Options.Factorio>(context.Configuration.GetSection("Factorio"));
                })
                .Build();

            await host.RunAsync();
        }
    }
}
