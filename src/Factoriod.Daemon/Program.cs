using System.Text.Json;
using System.Text.Json.Serialization;
using Factoriod.Fetcher;
using Yoh.Text.Json.NamingPolicies;

namespace Factoriod.Daemon
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    options.JsonSerializerOptions.WriteIndented = true;
                    options.JsonSerializerOptions.AllowTrailingCommas = true;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHostedService<FactorioHostedService>();
            builder.Services.AddHttpClient<VersionFetcher>();
            builder.Services.AddHttpClient<ReleaseFetcher>();

            builder.Services.AddSingleton<FactorioProcess>();

            var configurationDirectory = Environment.GetEnvironmentVariable("CONFIGURATION_DIRECTORY");
            var cacheDirectory = Environment.GetEnvironmentVariable("CACHE_DIRECTORY");
            var stateDirectory = Environment.GetEnvironmentVariable("STATE_DIRECTORY");
            if (configurationDirectory is not null)
            {
                builder.Configuration.AddJsonFile(Path.Combine(configurationDirectory, "appsettings.json"), optional: true, reloadOnChange: true);
                builder.Configuration.AddJsonFile(Path.Combine(configurationDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: true);
            }

            builder.Services.AddOptions<Options.Factorio>()
                .Configure(options =>
                {
                    options.Executable = new Options.FactorioExecutable
                    {
                        DownloadDirectory = cacheDirectory ?? string.Empty,
                    };

                    options.Configuration = new Options.FactorioConfiguration
                    {
                        RootDirectory = configurationDirectory ?? string.Empty,
                    };

                    options.Saves = new Options.FactorioSaves
                    {
                        RootDirectory = stateDirectory ?? string.Empty,
                    };

                    options.ModsRootDirectory = Path.Combine(cacheDirectory ?? string.Empty, "mods");
                })
                .BindConfiguration("Factorio")
                .ValidateDataAnnotations();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
