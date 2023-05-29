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

            if (!cancellationToken.IsCancellationRequested && exitCode != 0)
            {
                this.logger.LogError("Factorio process exited early with code {exitCode}, shutting down", exitCode);
                this.lifetime.StopApplication();
                return;
            }
        }
    }
}
