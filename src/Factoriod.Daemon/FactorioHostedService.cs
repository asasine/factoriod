using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Factoriod.Daemon
{
    public class FactorioHostedService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly FactorioOptions options;
        private readonly Process factorioProcess;

        public FactorioHostedService(ILogger<FactorioHostedService> logger, IOptions<FactorioOptions> options)
        {
            this.logger = logger;
            this.options = options.Value;

            this.logger.LogInformation("RootDirectory: {RootDirectory}", this.options.RootDirectory);
            this.logger.LogInformation("ExecutableRelativePath: {ExecutableRelativePath}", this.options.ExecutableRelativeDirectory);
            var factorioPath = Path.Combine(this.options.RootDirectory, this.options.ExecutableRelativeDirectory, "factorio");
            this.logger.LogInformation("Using factorio executable at {path}", factorioPath);
            this.factorioProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = factorioPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                },
            };
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Starting factorio process");
            this.factorioProcess.Start();
            await this.factorioProcess.WaitForExitAsync(cancellationToken);
        }
    }
}
