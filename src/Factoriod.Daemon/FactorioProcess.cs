using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Factoriod.Fetcher;
using Factoriod.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Factoriod.Daemon;

public class FactorioProcess
{
    private readonly ILogger<FactorioProcess> logger;
    private Options.Factorio options;
    private readonly VersionFetcher versionFetcher;
    private readonly ReleaseFetcher releaseFetcher;

    public FactorioProcess(ILogger<FactorioProcess> logger, IOptions<Options.Factorio> options, VersionFetcher versionFetcher, ReleaseFetcher releaseFetcher)
    {
        this.logger = logger;
        this.options = options.Value;
        this.versionFetcher = versionFetcher;
        this.releaseFetcher = releaseFetcher;
    }

    public async Task<int> StartServerAsync(CancellationToken cancellationToken = default)
    {
        // find a downloaded factorio
        //  if a version doesn't exist, download it
        //  if an outdated version exists, update it
        // start the server
        //  if a save doesn't exist, create one

        var factorioDirectory = await GetFactorioDirectoryAsync(cancellationToken);
        if (factorioDirectory == null)
        {
            this.logger.LogWarning("Unable to find Factorio directory");
            return 1;
        }

        this.logger.LogInformation("Starting factorio process");

        var arguments = new List<string>();
        var addedStartServer = AddArgumentIfFileExists(arguments, "--start-server", this.options.Saves.GetSavePath());
        if (!addedStartServer)
        {
            this.logger.LogInformation("No save file found, creating one");
            await CreateSaveIfNotExists(factorioDirectory, cancellationToken);

            // try again
            addedStartServer = AddArgumentIfFileExists(arguments, "--start-server", this.options.Saves.GetSavePath());
            if (!addedStartServer)
            {
                this.logger.LogError("Unable to find save file {path}", this.options.Saves.GetSavePath());
                return 1;
            }
        }

        AddServerSettingsArguments(arguments);
        AddServerPlayerListsArguments(arguments);
        AddModsArguments(arguments);

        using var factorioProcess = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(factorioDirectory.FullName, this.options.Executable.ExecutableDirectory, this.options.Executable.ExecutableName),
                Arguments = string.Join(" ", arguments),
                WorkingDirectory = factorioDirectory.FullName,
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

    /// <summary>
    /// Gets the path to the Factorio directory to use.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the process early.</param>
    /// <returns>The path to the Factorio directory.</returns>
    private async Task<DirectoryInfo?> GetFactorioDirectoryAsync(CancellationToken cancellationToken = default)
    {
        // get the desired version from configuration
        // return a path to a matching version on disk
        //  find a downloaded factorio binary and get its version
        //  if one doesn't exist, download the desired version
        //  if it does exist and its version does not match the desired version, update it

        var requestedVersion = await GetRequestedVersionAsync(cancellationToken);
        if (requestedVersion == null)
        {
            this.logger.LogWarning("Could not determine a requested version.");
            return null;
        }

        this.logger.LogDebug("Using Factorio version {version}", requestedVersion);

        var factorioDirectory = this.options.Executable.GetFactorioDirectory();
        this.logger.LogDebug("Checking for Factorio executables in {directory}", factorioDirectory);

        var versionOnDisk = await this.versionFetcher.GetVersionAsync(factorioDirectory, cancellationToken);
        if (versionOnDisk == null)
        {
            this.logger.LogInformation("Factorio version {version} not found on disk, downloading", requestedVersion);
            return await downloadRequestedVersionAsync();
        }

        this.logger.LogDebug("Found Factorio on disk: {versionOnDisk}", versionOnDisk);
        if (versionOnDisk.Value.Version.Version == requestedVersion)
        {
            this.logger.LogInformation("Version on disk matches requested version {version}.", requestedVersion);
            return versionOnDisk.Value.Path;
        }
        else if (versionOnDisk.Value.Version.Version > requestedVersion)
        {
            this.logger.LogInformation("Version {versionOnDisk} on disk is greater than requested version {requestedVersion}, downgrading it.", versionOnDisk.Value.Version.Version, requestedVersion);

            // downgrade by downloading a new version
            return await downloadRequestedVersionAsync();
        }
        else
        {
            this.logger.LogInformation("Version {versionOnDisk} on disk is less than requested version {requestedVersion}, upgrading it.", versionOnDisk.Value.Version.Version,requestedVersion);
            var updated = await UpdateToVersionAsync(versionOnDisk.Value, requestedVersion, cancellationToken);
            if (!updated)
            {
                this.logger.LogInformation("Unable to update existing download, downloading {version} directly instead.", requestedVersion);
                return await downloadRequestedVersionAsync();

            }
            return versionOnDisk.Value.Path;
        }

        async Task<DirectoryInfo?> downloadRequestedVersionAsync()
        {
            // downgrade by downloading a new version
            var downloadedDirectory = await this.releaseFetcher.DownloadToAsync(
                new FactorioVersion(requestedVersion, ReleaseBuild.Headless, !this.options.Executable.UseExperimental),
                Distro.Linux64,
                this.options.Executable.GetDownloadDirectory(),
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

    /// <summary>
    /// Gets the requested version from <see cref="options"/>.
    /// </summary>
    /// <remarks>
    /// If the version in <see cref="Options.FactorioExecutable.Version"/> is <c>latest</c>,
    /// fetches the latest version from the Factorio Version API.
    /// </remarks>
    /// <param name="cancellationToken">A token to cancel the task.</param>
    /// <returns>The requested version, or <see langword="null"/> if it could not be determined.</returns>
    private async Task<Version?> GetRequestedVersionAsync(CancellationToken cancellationToken = default)
    {
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

        return Version.Parse(unparsedRequestedVersion);
    }

    private async Task<bool> UpdateToVersionAsync(FactorioDirectory versionOnDisk, Version requestedVersion, CancellationToken cancellationToken = default)
    {
        // the update path is a series of updates from the version on disk to the requested version
        // this may include several updates applied in sequence
        var updatePath = await this.versionFetcher.GetUpdatePathAsync(versionOnDisk.Version.Version, requestedVersion, cancellationToken);

        if (updatePath == null)
        {
            this.logger.LogDebug("No update was found for update from {versionOnDisk} to {requestedVersion}", versionOnDisk.Version.Version, requestedVersion);
            return false;
        }

        if (!updatePath.Any())
        {
            this.logger.LogDebug("No update was required for update from {versionOnDisk} to {requestedVersion}", versionOnDisk.Version.Version, requestedVersion);
            return true;
        }

        var updateDirectory = this.options.Executable.GetUpdatesDirectory();
        var downloadTasks = updatePath.Select(update => this.releaseFetcher.DownloadUpdateAsync(update, updateDirectory, cancellationToken)).ToArray();
        var downloadedUpdates = (await Task.WhenAll(downloadTasks))
            .Where(update => update != null)
            .Select(update => update!.Value) // null forgiving because the previous .Where makes this safe
            .ToArray();

        if (downloadedUpdates.Length != downloadTasks.Length)
        {
            this.logger.LogWarning("Failed to download all updates required for update from {versionOnDisk} to {requestedVersion}", versionOnDisk.Version.Version, requestedVersion);
            return false;
        }

        // these must be applied in order, not concurrently
        foreach (var update in downloadedUpdates)
        {
            var success = await ApplyUpdateAsync(versionOnDisk, update, cancellationToken);
            if (success)
            {
                this.logger.LogDebug("Updated from {from} to {to}", update.FromTo.From, update.FromTo.To);
            }
            else
            {
                this.logger.LogWarning("Failed toa apply update from {from} to {to}", update.FromTo.From, update.FromTo.To);
                return false;
            }
        }

        foreach (var update in downloadedUpdates)
        {
            update.File.Delete();
        }

        return true;
    }

    private async Task<bool> ApplyUpdateAsync(FactorioDirectory versionOnDisk, FactorioUpdate update, CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "--apply-update",
            update.File.FullName,
        };

        using var updateProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = this.options.Executable.GetExecutablePath(versionOnDisk.Path).FullName,
                Arguments = string.Join(" ", arguments),
                WorkingDirectory = versionOnDisk.Path.FullName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            },
        };

        updateProcess.Start();
        await updateProcess.WaitForExitAsync(cancellationToken);
        if (updateProcess.ExitCode != 0)
        {
            this.logger.LogDebug($"{nameof(ApplyUpdateAsync)} for file {{file}} stdout:\n{{stdout}}", update, await updateProcess.StandardOutput.ReadToEndAsync());
            this.logger.LogDebug($"{nameof(ApplyUpdateAsync)} for file {{file}} stderr:\n{{stdout}}", update, await updateProcess.StandardError.ReadToEndAsync());
            return false;
        }

        return true;
    }

    private async Task CreateSaveIfNotExists(DirectoryInfo factorioDirectory, CancellationToken cancellationToken = default)
    {
        var savePath = this.options.Saves.GetSavePath();
        if (savePath.Exists)
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
                savePath.FullName,
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
                FileName = this.options.Executable.GetExecutablePath(factorioDirectory).FullName,
                Arguments = string.Join(" ", arguments),
                WorkingDirectory = factorioDirectory.FullName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            },
        };

        await StartProcessWithOutputHandlersAndWaitForExitAsync(createSaveProcess, cancellationToken);
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

        // TODO(#25): something is causing the last of the output to be lost and not sent to OnFactorioProcessOutputDataReceived
        await process.WaitForExitAsync(cancellationToken);

        process.CancelOutputRead();
        process.CancelErrorRead();
    }

    private void AddServerSettingsArguments(List<string> arguments)
    {
        var serverSettingsPath = this.options.Configuration.GetServerSettingsPath();
        if (serverSettingsPath.Exists)
        {
            using var serverSettingsStream = serverSettingsPath.OpenRead();
            var serverSettings = JsonNode.Parse(serverSettingsStream);
            if (serverSettings != null)
            {
                this.logger.LogInformation("Creating game '{name}': {description}", serverSettings["name"], serverSettings["description"]);
            }
        }

        var exists = AddArgumentIfFileExists(arguments, "--server-settings", serverSettingsPath);
        if (!exists)
        {
            this.logger.LogInformation("No server settings were found, using default values.");
        }
    }

    private void AddServerPlayerListsArguments(List<string> arguments)
    {
        var addedWhitelist = AddArgumentIfFileExists(arguments, "--server-whitelist", this.options.Configuration.GetServerWhitelistPath());
        if (addedWhitelist)
        {
            arguments.Add("--use-server-whitelist");
        }

        AddArgumentIfFileExists(arguments, "--server-banlist", this.options.Configuration.GetServerBanlistPath());
        AddArgumentIfFileExists(arguments, "--server-adminlist", this.options.Configuration.GetServerAdminlistPath());
    }

    private void AddModsArguments(List<string> arguments)
    {
        AddArgumentIfDirectoryExists(arguments, "--mod-directory", this.options.GetModsRootDirectory());
    }

    private static bool AddArgumentIfFileExists(List<string> arguments, string option, FileInfo path)
    {
        if (path.Exists)
        {
            arguments.Add(option);
            arguments.Add(path.FullName);
            return true;
        }

        return false;
    }

    private static bool AddArgumentIfDirectoryExists(List<string> arguments, string option, DirectoryInfo path)
    {
        if (path.Exists)
        {
            arguments.Add(option);
            arguments.Add(path.FullName);
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

        this.logger.LogTrace("Factorio process output: {output}", e.Data);
        var badVersionMatch = Regex.Match(e.Data, @"Map version (?<new_version>\d+\.\d+\.\d+)-0 cannot be loaded because it is higher than the game version \((?<old_version>\d+\.\d+\.\d+)-0\)");
        if (badVersionMatch.Success)
        {
            // TODO(#24): Handle newer maps being loaded by older server versions
            this.logger.LogWarning("Factorio map version {new_version} cannot be loaded because it is higher than the game version {old_version}", badVersionMatch.Groups["new_version"].Value, badVersionMatch.Groups["old_version"].Value);
        }

        if (e.Data.Contains("changing state from(CreatingGame) to(InGame)"))
        {
            this.logger.LogInformation("Factorio process started");
        }

        var userJoinLeave = Regex.Match(e.Data, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2} \[(?:JOIN|LEAVE)] (?<user>\w+) (?<action>joined|left) the game$");
        if (userJoinLeave.Success)
        {
            this.logger.LogInformation("{user} {action} the game", userJoinLeave.Groups["user"].Value, userJoinLeave.Groups["action"].Value);
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
}
