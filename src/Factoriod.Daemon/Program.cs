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

            // register FactorioProcess as a hosted service and a singleton so services can access its public methods too
            builder.Services.AddHostedService<FactorioProcess>();
            builder.Services.AddSingleton<FactorioProcess>();

            builder.Services.AddHttpClient<VersionFetcher>();
            builder.Services.AddHttpClient<ReleaseFetcher>();

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
                        RootDirectory = cacheDirectory ?? string.Empty,
                    };

                    options.Configuration = new Options.FactorioConfiguration
                    {
                        RootDirectory = Path.Combine(stateDirectory ?? string.Empty, "config", "factorio"),
                    };

                    options.Saves = new Options.FactorioSaves
                    {
                        RootDirectory = Path.Combine(stateDirectory ?? string.Empty, "saves"),
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
