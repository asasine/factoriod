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

            // download (or use cached) executable matching the configured version
            // start the server

            var factorioDirectory = await GetFactorioDirectoryAsync(cancellationToken);
            if (factorioDirectory == null)
            {
                this.logger.LogWarning("Unable to find Factorio directory");
                this.lifetime.StopApplication();
                return;
            }

            var exitCode = await this.factorioProcess.StartServerAsync(factorioDirectory, cancellationToken);
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
        private async Task<DirectoryInfo?> GetFactorioDirectoryAsync(CancellationToken cancellationToken = default)
        {
            // get the desired version from configuration
            // if that version is "latest", get the latest version from the API
            // if that desired version (configured or "latest") is on disk, return the path to it
            // else, download the version to disk and return the path to it

            var executableDownloadDirectory = this.options.Executable.GetDownloadDirectory();

            this.logger.LogDebug("Checking for Factorio executables in {executableDownloadDirectory} ({config})", executableDownloadDirectory, this.options.Executable.DownloadDirectory);

            var unparsedRequestedVersion = this.options.Executable.Version;
            if (unparsedRequestedVersion == "latest")
            {
                var latestVersion = await this.versionFetcher.GetLatestHeadlessVersionAsync(this.options.Executable.UseExperimental, cancellationToken);
                if (latestVersion == null)
                {
                    return null;
                }

                unparsedRequestedVersion = latestVersion.Version.ToString();
            }

            var requestedVersion = Version.Parse(unparsedRequestedVersion);
            this.logger.LogDebug("Using Factorio version {version}", requestedVersion);

            var versionsOnDisk = await this.versionFetcher.GetVersionsOnDiskAsync(executableDownloadDirectory, cancellationToken);
            if (versionsOnDisk == null)
            {
                return null;
            }

            foreach (var versionOnDisk in versionsOnDisk)
            {
                this.logger.LogDebug("Found Factorio on disk: {versionOnDisk}", versionOnDisk);
                if (versionOnDisk.Version.Build == ReleaseBuild.Headless && versionOnDisk.Version.Version == requestedVersion)
                {
                    this.logger.LogInformation("Using Factorio version on disk {versionOnDisk}", versionOnDisk);
                    return versionOnDisk.Path;
                }
            }

            this.logger.LogInformation("Factorio version {version} not found on disk, downloading", requestedVersion);
            var downloadedDirectory = await this.releaseFetcher.DownloadToAsync(
                new FactorioVersion(requestedVersion, ReleaseBuild.Headless, !this.options.Executable.UseExperimental),
                Distro.Linux64,
                executableDownloadDirectory,
                cancellationToken);

            if (downloadedDirectory == null)
            {
                this.logger.LogError("Download failed");
                return null;
            }

            this.logger.LogDebug("Downloaded Factorio version {version} to {path}", requestedVersion, downloadedDirectory);
            return downloadedDirectory;
        }
    }
}
