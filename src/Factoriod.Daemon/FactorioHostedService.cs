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

            var factorioExecutablePath = Path.Combine(this.options.RootDirectory, this.options.ExecutableRelativePath);
            this.logger.LogInformation("Using factorio executable at {path}", factorioExecutablePath);
            this.factorioProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = factorioExecutablePath,
                    Arguments = "--start-server saves/save1.zip",
                    WorkingDirectory = this.options.RootDirectory,
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

            this.factorioProcess.OutputDataReceived += OnFactorioProcessOutputDataReceived;
            this.factorioProcess.ErrorDataReceived += OnFactorioProcessErrorDataReceived;

            this.factorioProcess.Start();

            this.factorioProcess.BeginOutputReadLine();
            this.factorioProcess.BeginErrorReadLine();

            await this.factorioProcess.WaitForExitAsync(cancellationToken);

            this.factorioProcess.CancelOutputRead();
            this.factorioProcess.CancelErrorRead();

            this.logger.LogInformation("Factorio process exit code: {exitCode}", this.factorioProcess.ExitCode);
        }

        public override void Dispose()
        {
            this.factorioProcess.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnFactorioProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }
            
            this.logger.LogInformation("Factorio process output: {output}", e.Data);
        }

        private void OnFactorioProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            this.logger.LogWarning("Factorio process error: {error}", e.Data);
        }
    }
}
