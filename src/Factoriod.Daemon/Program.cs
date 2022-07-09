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
                    var configurationDirectory = Environment.GetEnvironmentVariable("CONFIGURATION_DIRECTORY");
                    if (configurationDirectory != null)
                    {
                        config.AddJsonFile(Path.Combine(configurationDirectory, "appsettings.json"), optional: true);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<FactorioHostedService>();
                    services.AddHttpClient<VersionFetcher>();
                    services.AddHttpClient<ReleaseFetcher>();

                    services.AddOptions<Options.Factorio>() 
                        .Configure(options =>
                        {
                            options.Executable = new Options.FactorioExecutable
                            {
                                DownloadDirectory = Environment.GetEnvironmentVariable("CACHE_DIRECTORY")!,
                            };

                            options.Configuration = new Options.FactorioConfiguration
                            {
                                RootDirectory = Environment.GetEnvironmentVariable("CONFIGURATION_DIRECTORY")!,
                            };

                            options.Saves = new Options.FactorioSaves
                            {
                                RootDirectory = Environment.GetEnvironmentVariable("STATE_DIRECTORY")!,
                            };

                            options.MapGeneration = new Options.FactorioMapGeneration
                            {
                                RootDirectory = Environment.GetEnvironmentVariable("CONFIGURATION_DIRECTORY")!,
                            };
                        })
                        .Bind(context.Configuration.GetSection("Factorio"))
                        .ValidateDataAnnotations();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
