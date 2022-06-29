using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Factoriod.Daemon
{
    public class FactorioHostedService : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IHostApplicationLifetime lifetime;
        private readonly Options.Factorio options;
        private readonly Process factorioProcess;

        public FactorioHostedService(ILogger<FactorioHostedService> logger, IOptions<Options.Factorio> options, IHostApplicationLifetime lifetime)
        {
            this.logger = logger;
            this.lifetime = lifetime;
            this.options = options.Value;

            var factorioExecutablePath = Path.Combine(this.options.Executable.RootDirectory, this.options.Executable.ExecutableName);
            this.logger.LogInformation("Using factorio executable at {path}", factorioExecutablePath);
            this.factorioProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = factorioExecutablePath,
                    Arguments = CreateArguments(),
                    WorkingDirectory = this.options.Executable.RootDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                },
            };
        }

        private string CreateArguments()
        {
            // /etc/factoriod/server-settings.json
            var arguments = new List<string>();
            AddArgumentIfFileExists(arguments, "--start-server", this.options.Configuration.GetSavePath());
            AddArgumentIfFileExists(arguments, "--server-settings", this.options.Configuration.GetServerSettingsPath());
            var addedWhitelist = AddArgumentIfFileExists(arguments, "--server-whitelist", this.options.Configuration.GetServerWhitelistPath());
            if (addedWhitelist)
            {
                arguments.Add("--use-server-whitelist");
            }

            AddArgumentIfFileExists(arguments, "--server-banlist", this.options.Configuration.GetServerBanlistPath());
            AddArgumentIfFileExists(arguments, "--server-adminlist", this.options.Configuration.GetServerAdminlistPath());
            AddArgumentIfDirectoryExists(arguments, "--mod-directory", this.options.ModsRootDirectory);

            return string.Join(" ", arguments);
        }

        private static bool AddArgumentIfFileExists(List<string> arguments, string option, string path)
        {
            if (File.Exists(path))
            {
                arguments.Add(option);
                arguments.Add(path);
                return true;
            }

            return false;
        }

        private static bool AddArgumentIfDirectoryExists(List<string> arguments, string option, string path)
        {
            if (Directory.Exists(path))
            {
                arguments.Add(option);
                arguments.Add(path);
                return true;
            }

            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Don't start the process if the app is being stopped
                return;
            }

            this.logger.LogInformation("Starting factorio process");

            this.factorioProcess.OutputDataReceived += OnFactorioProcessOutputDataReceived;
            this.factorioProcess.ErrorDataReceived += OnFactorioProcessErrorDataReceived;

            this.factorioProcess.Start();

            this.factorioProcess.BeginOutputReadLine();
            this.factorioProcess.BeginErrorReadLine();

            await this.factorioProcess.WaitForExitAsync(cancellationToken);

            this.factorioProcess.CancelOutputRead();
            this.factorioProcess.CancelErrorRead();

            if (!cancellationToken.IsCancellationRequested && this.factorioProcess.ExitCode != 0)
            {
                this.logger.LogError("Factorio process exited early with code {exitCode}, shutting down", this.factorioProcess.ExitCode);
                this.lifetime.StopApplication();
            }
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
