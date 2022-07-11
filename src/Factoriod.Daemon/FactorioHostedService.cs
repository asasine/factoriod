using Factoriod.Fetcher;
using Factoriod.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Factoriod.Daemon
{
    public class FactorioHostedService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly Options.Factorio options;
        private readonly IHostApplicationLifetime lifetime;
        private readonly VersionFetcher versionFetcher;
        private readonly ReleaseFetcher releaseFetcher;
        private readonly FactorioProcess factorioProcess;

        public FactorioHostedService(ILogger<FactorioHostedService> logger, IOptions<Options.Factorio> options,
            IHostApplicationLifetime lifetime, VersionFetcher versionFetcher, ReleaseFetcher releaseFetcher, FactorioProcess factorioProcess)
        {
            this.logger = logger;
            this.options = options.Value;
            this.lifetime = lifetime;
            this.versionFetcher = versionFetcher;
            this.releaseFetcher = releaseFetcher;
            this.factorioProcess = factorioProcess;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Don't start the process if the app is being stopped
                return;
            }

            var exitCode = await this.factorioProcess.StartServerAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested && exitCode != 0)
            {
                this.logger.LogError("Factorio process exited early with code {exitCode}, shutting down", exitCode);
                this.lifetime.StopApplication();
                return;
            }
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            // wait up to 5 seconds for the running task to complete
            await Task.WhenAny(ExecuteTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            await base.StopAsync(cancellationToken);
        }
    }
}
