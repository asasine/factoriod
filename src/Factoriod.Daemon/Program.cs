using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Factoriod.Daemon
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
                .UseContentRoot(Path.GetDirectoryName(typeof(Program).Assembly.Location))
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<FactorioHostedService>();

                    services.Configure<FactorioOptions>(context.Configuration.GetSection("Factorio"));
                })
                .Build();

            await host.RunAsync();
        }
    }
}
