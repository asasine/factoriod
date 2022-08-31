using Factoriod.Fetcher;

namespace Factoriod.Daemon
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //builder.Services.AddHostedService<FactorioHostedService>();
            builder.Services.AddHttpClient<VersionFetcher>();
            builder.Services.AddHttpClient<ReleaseFetcher>();

            builder.Services.AddTransient<FactorioProcess>();

            builder.Services.AddOptions<Options.Factorio>()
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
                .Bind(builder.Configuration.GetSection("Factorio"))
                .ValidateDataAnnotations();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
