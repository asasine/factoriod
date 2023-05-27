using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Factoriod.Daemon.Models;
using Factoriod.Fetcher;
using Factoriod.Models;
using Microsoft.Extensions.Options;
using Mono.Unix.Native;

namespace Factoriod.Daemon;

public sealed class FactorioProcess : IDisposable
{
    private readonly ILogger<FactorioProcess> logger;
    private readonly Options.Factorio options;
    private readonly VersionFetcher versionFetcher;
    private readonly ReleaseFetcher releaseFetcher;

    public ServerStatus ServerStatus { get; init; }

    /// <summary>
    /// Set to a value if the factorio process shuts down due to an error state.
    /// </summary>
    /// <remarks>
    /// This often occurs from loading a newer version of the map with an older version of the game.
    /// See <see cref="FactorioException"/> and derived types for possible faults.
    /// </remarks>
    private FactorioException? incompatibleMapVersionError = null;

    /// <summary>
    /// A cancellation token source that can be cancelled to restart the factorio process.
    /// </summary>
    private CancellationTokenSource serverRestartCts = new();

    public FactorioProcess(ILogger<FactorioProcess> logger, IOptions<Options.Factorio> options, VersionFetcher versionFetcher, ReleaseFetcher releaseFetcher)
    {
        this.logger = logger;
        this.options = options.Value;
        this.versionFetcher = versionFetcher;
        this.releaseFetcher = releaseFetcher;
        this.ServerStatus = new ServerStatus();
    }

    public void Dispose()
    {
        this.serverRestartCts.Dispose();
    }

    public async Task<int> StartServerAsync(CancellationToken serverStoppingToken = default)
    {
        // find a downloaded factorio
        //  if a version doesn't exist, download it
        //  if an outdated version exists, update it
        // start the server
        //  if a save doesn't exist, create one
        while (true)
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverStoppingToken, this.serverRestartCts.Token);
            var cancellationToken = cancellationTokenSource.Token;
            this.ServerStatus.ServerState = ServerState.Launching;
            var factorioDirectory = await GetFactorioDirectoryAsync(cancellationToken);
            if (factorioDirectory == null)
            {
                this.logger.LogWarning("Unable to find Factorio directory");
                return 2;
            }

            this.logger.LogInformation("Starting factorio process");

            var arguments = new List<string>();
            var savePath = await SelectOrCreateSave(factorioDirectory, cancellationToken);
            if (savePath == null)
            {
                this.logger.LogWarning("Could not create save file.");
                return 2;
            }

            var addedStartServer = AddArgumentIfFileExists(arguments, "--start-server", savePath);
            if (!addedStartServer)
            {
                this.logger.LogError("Unable to find save file {path}", savePath);
                return 2;
            }

            this.logger.LogInformation("Using save {name} (path: {file})", savePath.Name, savePath);

            AddServerSettingsArguments(arguments);
            AddServerPlayerListsArguments(arguments);
            AddModsArguments(arguments);

            var saveBackup = BackupFile(savePath);
            if (saveBackup == null)
            {
                this.logger.LogWarning("Failed to create backup for save file {path}", savePath);
            }

            using var factorioProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(factorioDirectory.FullName, this.options.Executable.ExecutableDirectory, this.options.Executable.ExecutableName),
                    Arguments = string.Join(' ', arguments),
                    WorkingDirectory = factorioDirectory.FullName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                },
            };

            this.ServerStatus.SetRunning(new Save(savePath.FullName));
            await StartProcessWithOutputHandlersAndWaitForExitAsync(factorioProcess, cancellationToken);
            this.ServerStatus.ServerState = ServerState.Exited;

            if (incompatibleMapVersionError != null)
            {
                this.ServerStatus.SetFaulted(incompatibleMapVersionError);
                this.logger.LogError("Could not run factorio", incompatibleMapVersionError);

                return 2;
            }
            else
            {
                this.ServerStatus.ServerState = ServerState.Exited;
            }

            if (this.serverRestartCts.IsCancellationRequested)
            {
                this.logger.LogInformation("Server restart requested, restarting");
                var oldCts = Interlocked.Exchange(ref this.serverRestartCts, new CancellationTokenSource());
                oldCts.Dispose();
            }

            if (serverStoppingToken.IsCancellationRequested || factorioProcess.HasExited)
            {
                // factorio process returns exit code 1 when its shutdown, consider this a successful code
                if (factorioProcess.ExitCode == 1)
                {
                    this.logger.LogTrace("Factorio process exited with code 1 (early shutdown), masking as 0.");
                    return 0;
                }

                return factorioProcess.ExitCode;
            }
        }
    }

    /// <summary>
    /// Restarts the executing factorio process with the provided <paramref name="save"/>.
    /// </summary>
    /// <param name="save">The new save to start the factorio process with.</param>
    /// <exception cref="NotImplementedException">The method has not been implemented yet.</exception>
    /// <exception cref="FileNotFoundException">The save file was not found.</exception>
    public void SetSave(Save save)
    {
        if (!File.Exists(save.Path))
        {
            throw new FileNotFoundException("Could not find provided save file", save.Path);
        }

        this.logger.LogInformation("Setting current save to {save}", save);
        this.options.Saves.SetCurrentSavePath(new FileInfo(save.Path));
        this.serverRestartCts.Cancel();
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
            this.logger.LogInformation("Version {versionOnDisk} on disk is less than requested version {requestedVersion}, upgrading it.", versionOnDisk.Value.Version.Version, requestedVersion);
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
        this.logger.LogDebug("Requested version: {version}", unparsedRequestedVersion);
        if (unparsedRequestedVersion == "latest")
        {
            this.logger.LogDebug("Getting latest version from Factorio API");
            var latestVersion = await this.versionFetcher.GetLatestHeadlessVersionAsync(this.options.Executable.UseExperimental, cancellationToken);
            if (latestVersion == null)
            {
                return null;
            }

            return latestVersion.Version;
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
                Arguments = string.Join(' ', arguments),
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

    /// <summary>
    /// Selects a save file to use, or creates one if none exist.
    /// The selected save file is the path specified in
    /// </summary>
    /// <param name="factorioDirectory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<FileInfo?> SelectOrCreateSave(DirectoryInfo factorioDirectory, CancellationToken cancellationToken = default)
    {
        var savePath = this.options.Saves.GetCurrentSavePath();
        if (savePath != null && savePath.Exists)
        {
            this.logger.LogDebug("Found save file {path}", savePath);
            return savePath;
        }

        if (savePath == null)
        {
            var savesRootDirectory = this.options.Saves.GetRootDirectory();

            // ensure it's created, otherwise a DirectoryNotFoundException is thrown
            savesRootDirectory.Create();

            // choose the save which was modified most recently
            savePath = savesRootDirectory.EnumerateFiles().MaxBy(file => file.LastWriteTimeUtc);
        }

        if (savePath != null && savePath.Exists)
        {
            this.logger.LogDebug("Found newest save file {path}", savePath);
            this.options.Saves.SetCurrentSavePath(savePath);
            return savePath;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogDebug("Cancellation requested, not creating save.");
            return null;
        }

        if (savePath == null)
        {
            // no save path specified and no save files found
            savePath = new FileInfo(Path.Combine(this.options.Saves.GetRootDirectory().FullName, "save1.zip"));
            this.logger.LogDebug("No save file found, creating {path}", savePath);
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
                Arguments = string.Join(' ', arguments),
                WorkingDirectory = factorioDirectory.FullName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
            },
        };

        await StartProcessWithOutputHandlersAndWaitForExitAsync(createSaveProcess, cancellationToken);

        savePath.Refresh();
        this.options.Saves.SetCurrentSavePath(savePath);
        return savePath;
    }

    /// <summary>
    /// Backs up a file by copying it to a new file with the specified extension.
    /// Any file at the backup path will be overwritten.
    /// </summary>
    /// <param name="file">The file to backup.</param>
    /// <param name="extension">The extension to append to the filename.</param>
    /// <returns><see langword="true"/> if the file was backed up successfully, otherwise <see langword="false"/>.</returns>
    private static FileInfo? BackupFile(FileInfo file, string extension = ".bak")
    {
        if (file.Directory == null)
        {
            return null;
        }

        if (!file.Exists)
        {
            return null;
        }

        extension = extension.Trim('.');

        var backupPath = new FileInfo(Path.Combine(file.Directory.FullName, $"{file.Name}.{extension}"));
        if (backupPath.Exists)
        {
            backupPath.Delete();
        }

        return file.CopyTo(backupPath.FullName, true);
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

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // System.Diagnostics.Process doesn't send SIGTERM to the process when cancelled, so we have to do it ourselves
            this.logger.LogInformation("Cancellation requested, sending SIGTERM to process {pid}", process.Id);
            Syscall.kill(process.Id, Signum.SIGTERM);
        }

        if (cancellationToken.IsCancellationRequested && !process.HasExited)
        {
            this.logger.LogDebug("SIGTERM sent, waiting 5s before SIGKILL.");
            using var sigkillCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await process.WaitForExitAsync(sigkillCts.Token);
            }
            catch (TaskCanceledException)
            {
            }
        }

        if (!process.HasExited)
        {
            this.logger.LogWarning("Process did not exit after SIGTERM, sending SIGKILL.");
            process.Kill();
        }

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
            var newVersion = Version.Parse(badVersionMatch.Groups["new_version"].Value);
            var oldVersion = Version.Parse(badVersionMatch.Groups["old_version"].Value);
            this.logger.LogWarning("Factorio map version {new_version} cannot be loaded because it is higher than the game version {old_version}", newVersion, oldVersion);
            incompatibleMapVersionError = new IncompatibleMapVersionException(oldVersion, newVersion, this.ServerStatus.Save);
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

    private record IncompatibleMapVersionError(Version OldVersion, Version NewVersion);
}
