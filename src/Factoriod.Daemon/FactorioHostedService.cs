using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Factoriod.Daemon
{
    public class FactorioHostedService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IHostApplicationLifetime lifetime;
        private readonly FactorioProcess factorioProcess;

        public FactorioHostedService(ILogger<FactorioHostedService> logger, IHostApplicationLifetime lifetime, FactorioProcess factorioProcess)
        {
            this.logger = logger;
            this.lifetime = lifetime;
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
            
            // factorio process returns exit code 1 when its shutdown by SIGTERM, consider this a successful code
            if (!cancellationToken.IsCancellationRequested && !(exitCode == 0 || exitCode == 1))
            {
                this.logger.LogError("Factorio process exited early with code {exitCode}, shutting down", exitCode);
                this.lifetime.StopApplication();
                return;
            }
        }

        public async override Task StopAsync(CancellationToken cancellationToken)
        {
            // wait up to 5 seconds for the running task to complete
            var infiniteDelay = Task.Delay(Timeout.Infinite, cancellationToken);
            var completed = await Task.WhenAny(ExecuteTask, infiniteDelay);
            if (completed == infiniteDelay)
            {
                this.logger.LogWarning("Unable to complete running task before ungraceful shutdown was requested.");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
