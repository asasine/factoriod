using System.Diagnostics;
using System.Text.RegularExpressions;
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

        public FactorioHostedService(ILogger<FactorioHostedService> logger, IOptions<Options.Factorio> options, IHostApplicationLifetime lifetime)
        {
            this.logger = logger;
            this.lifetime = lifetime;
            this.options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Don't start the process if the app is being stopped
                return;
            }

            // download (or use cached) executable matching the configured version
            // start the server

            var factorioDirectory = await GetFactorioDirectoryAsync(cancellationToken);
            if (factorioDirectory == null)
            {
                this.logger.LogWarning("Unable to find Factorio directory");
                this.lifetime.StopApplication();
                return;
            }

            var exitCode = await StartServerAsync(factorioDirectory, cancellationToken);
            if (!cancellationToken.IsCancellationRequested && exitCode != 0)
            {
                this.logger.LogError("Factorio process exited early with code {exitCode}, shutting down", exitCode);
                this.lifetime.StopApplication();
                return;
            }
        }

        /// <summary>
        /// Gets the path to the Factorio directory to use.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to stop the process early.</param>
        /// <returns>The path to the Factorio directory.</returns>
        private Task<string?> GetFactorioDirectoryAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(this.options.Executable.RootDirectory);
        }

        private async Task<int> StartServerAsync(string factorioDirectory, CancellationToken cancellationToken = default)
        {
            this.logger.LogInformation("Starting factorio process");

            var arguments = new List<string>();
            var addedStartServer = AddArgumentIfFileExists(arguments, "--start-server", this.options.Configuration.GetSavePath());
            if (!addedStartServer)
            {
                this.logger.LogInformation("No save file found, creating one");
                await CreateSaveIfNotExists(factorioDirectory, cancellationToken);

                // try again
                addedStartServer = AddArgumentIfFileExists(arguments, "--start-server", this.options.Configuration.GetSavePath());
                if (!addedStartServer)
                {
                    this.logger.LogError("Unable to find save file {path}", this.options.Configuration.GetSavePath());
                    return 1;
                }
            }

            AddArgumentIfFileExists(arguments, "--server-settings", this.options.Configuration.GetServerSettingsPath());
            var addedWhitelist = AddArgumentIfFileExists(arguments, "--server-whitelist", this.options.Configuration.GetServerWhitelistPath());
            if (addedWhitelist)
            {
                arguments.Add("--use-server-whitelist");
            }

            AddArgumentIfFileExists(arguments, "--server-banlist", this.options.Configuration.GetServerBanlistPath());
            AddArgumentIfFileExists(arguments, "--server-adminlist", this.options.Configuration.GetServerAdminlistPath());
            AddArgumentIfDirectoryExists(arguments, "--mod-directory", this.options.ModsRootDirectory);

            using var factorioProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Join(factorioDirectory, this.options.Executable.ExecutableDirectory, this.options.Executable.ExecutableName),
                    Arguments = string.Join(" ", arguments),
                    WorkingDirectory = factorioDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                },
            };

            await StartProcessWithOutputHandlersAndWaitForExitAsync(factorioProcess, cancellationToken);
            return factorioProcess.ExitCode;
        }

        private async Task CreateSaveIfNotExists(string factorioDirectory, CancellationToken cancellationToken = default)
        {
            var savePath = this.options.Configuration.GetSavePath();
            if (File.Exists(savePath))
            {
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            this.logger.LogInformation("Creating save file {path}", savePath);

            var arguments = new List<string>()
            {
                "--create",
                savePath,
            };

            AddArgumentIfFileExists(arguments, "--map-gen-settings", this.options.MapGeneration.GetMapGenSettingsPath());
            AddArgumentIfFileExists(arguments, "--map-settings", this.options.MapGeneration.GetMapSettingsPath());
            if (this.options.MapGeneration.MapGenSeed.HasValue)
            {
                arguments.Add("--map-gen-seed");
                arguments.Add(this.options.MapGeneration.MapGenSeed.Value.ToString());
            }
            
            using var createSaveProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = this.options.Executable.GetExecutablePath(factorioDirectory),
                    Arguments = string.Join(" ", arguments),
                    WorkingDirectory = factorioDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                },
            };

            await StartProcessWithOutputHandlersAndWaitForExitAsync(createSaveProcess, cancellationToken);
        }

        private async Task<Version?> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = this.options.Executable.GetExecutablePath(),
                    Arguments = "--version",
                    WorkingDirectory = this.options.Executable.RootDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);
            // version info is on the first line
            var versionLine = await process.StandardOutput.ReadLineAsync();
            if (versionLine == null)
            {
                return null;
            }

            // a more complete regex, if distro and release are needed later
            // var versionMatch = Regex.Match(versionLine, @"^Version: (?<version>\d+\.\d+\.\d+) \(build \d+, (?<distro>win64|win64-manual|win32|win32-manual|osx|linux64|linux32), (?<release>alpha|demo|headless)\)$");
            var versionMatch = Regex.Match(versionLine, @"^Version: (?<version>\d+\.\d+\.\d+)");
            var version = versionMatch.Success ? new Version(versionMatch.Groups["version"].Value) : null;
            return version;
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

        private void OnFactorioProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            this.logger.LogDebug("Factorio process output: {output}", e.Data);

            if (e.Data.Contains("changing state from(CreatingGame) to(InGame)"))
            {
                this.logger.LogInformation("Factorio process started");
            }
        }

        private void OnFactorioProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                return;
            }

            this.logger.LogWarning("Factorio process error: {error}", e.Data);
        }

        private async Task StartProcessWithOutputHandlersAndWaitForExitAsync(Process process, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            process.OutputDataReceived += this.OnFactorioProcessOutputDataReceived;
            process.ErrorDataReceived += this.OnFactorioProcessErrorDataReceived;

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            process.CancelOutputRead();
            process.CancelErrorRead();
        }
    }
}
