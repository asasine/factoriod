using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Factoriod.Daemon.Models;
using Factoriod.Fetcher;
using Factoriod.Models;
using Factoriod.Models.Game;
using Factoriod.Utilities;
using Microsoft.Extensions.Options;
using Mono.Unix;
using Mono.Unix.Native;
using Yoh.Text.Json.NamingPolicies;

namespace Factoriod.Daemon;

public sealed class FactorioProcess : IHostedService, IDisposable
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
    /// A cancellation token source for the currently-running factorio process.
    /// </summary>
    private CancellationTokenSource processCts = new();

    /// <summary>
    /// The asynchronous operation which launched the factorio process.
    /// </summary>
    private Task? factorioTask = null;

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
        this.processCts.Dispose();
        this.factorioTask?.Dispose();
    }

    /// <summary>
    /// Start a new factorio process.
    /// If a process is already running, this method returns immediately.
    /// </summary>
    /// <param name="cancellationToken">Indicates when the start process should be aborted.</param>
    /// <returns>A task that represents the start operation. This does not include waiting for the factorio process to complete.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (this.factorioTask != null)
        {
            // previously started
            if (!this.factorioTask.IsCompleted)
            {
                // not completed, nothing to do
                return Task.CompletedTask;
            }
            else
            {
                // completed, restart it
                this.factorioTask.Dispose();
                this.factorioTask = null;
            }
        }

        this.processCts.Dispose();
        this.processCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this.factorioTask = StartServerAsync(this.processCts.Token);

        if (this.factorioTask.IsCompleted)
        {
            // the task completed synchronously, bubble up its cancellation and failures
            return this.factorioTask;
        }

        // otherwise, it's running, return a completed task
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the running factorio process.
    /// If no process is running, this method returns immediately.
    /// This method returns when the factorio process has exited or cancellation has been requested.
    /// </summary>
    /// <param name="cancellationToken">Indicates when shutdown should no longer be graceful.</param>
    /// <returns>A task that represents the stop operation, including waiting for the factorio process to exit.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.factorioTask == null || this.factorioTask.IsCompleted)
        {
            this.factorioTask?.Dispose();
            this.factorioTask = null;
            return;
        }

        try
        {
            this.processCts.Cancel();
        }
        finally
        {
            // wait for the task to finish, or cancellationToken to trigger
            var tcs = new TaskCompletionSource();
            using var ctr = cancellationToken.Register(s => ((TaskCompletionSource)s!).SetCanceled(), tcs);
            await Task.WhenAny(this.factorioTask, tcs.Task);
        }
    }

    /// <summary>
    /// Restarts the factorio process.
    /// Cancelling this operation may leave the factorio process stopped.
    /// </summary>
    /// <param name="cancellationToken">Indicates whether the restart should be aborted.</param>
    /// <returns>A task that represents the restart operation.</returns>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    private async Task<int> StartServerAsync(CancellationToken cancellationToken = default)
    {
        // find a downloaded factorio
        //  if a version doesn't exist, download it
        //  if an outdated version exists, update it
        // start the server
        //  if a save doesn't exist, create one
        if (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogDebug("Cancellation requested immediately after starting.");
            return 0;
        }

        this.ServerStatus.ServerState = ServerState.Launching;
        var factorioDirectory = await GetFactorioDirectoryAsync(cancellationToken);
        if (factorioDirectory == null)
        {
            this.logger.LogWarning("Unable to find Factorio directory");
            return 1;
        }

        this.logger.LogInformation("Starting factorio process");

        var arguments = new List<string>();
        var savePath = await SelectOrCreateSave(factorioDirectory, cancellationToken);
        if (savePath == null)
        {
            this.logger.LogWarning("Could not create save file.");
            return 1;
        }

        var addedStartServer = AddArgumentIfFileExists(arguments, "--start-server", savePath);
        if (!addedStartServer)
        {
            this.logger.LogError("Unable to find save file {path}", savePath);
            return 1;
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
        var exitCode = await StartProcessWithOutputHandlersAndWaitForExitAsync(factorioProcess, cancellationToken);
        this.ServerStatus.ServerState = ServerState.Exited;

        if (incompatibleMapVersionError != null)
        {
            this.ServerStatus.SetFaulted(incompatibleMapVersionError);
            this.logger.LogError("Could not run factorio", incompatibleMapVersionError);

            return 1;
        }

        if (factorioProcess.HasExited)
        {
            // factorio process returns exit code 1 when its shutdown, consider this a successful code
            if (factorioProcess.ExitCode == 1)
            {
                this.logger.LogTrace("Factorio process exited with code 1 (early shutdown), masking as 0.");
                return 0;
            }

            return factorioProcess.ExitCode;
        }
        
        if (cancellationToken.IsCancellationRequested)
        {
            if (!factorioProcess.HasExited)
            {
                this.logger.LogWarning("Server stopping requested, but factorio process has not exited.");
            }

            return 1;
        }

        this.logger.LogWarning("Neither the factorio process not exited nor has a cancellation been requested. How did we get here?");
        return -1;
    }

    /// <summary>
    /// Restarts the executing factorio process with the provided <paramref name="save"/>.
    /// </summary>
    /// <param name="save">The new save to start the factorio process with.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <exception cref="NotImplementedException">The method has not been implemented yet.</exception>
    /// <exception cref="FileNotFoundException">The save file was not found.</exception>
    public async Task SetSave(Save save, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(save.Path))
        {
            throw new FileNotFoundException("Could not find provided save file", save.Path);
        }

        this.logger.LogInformation("Setting current save to {save}", save);
        this.options.Saves.SetCurrentSavePath(new FileInfo(save.Path));
        await this.StopAsync(cancellationToken);
        await this.StartAsync(cancellationToken);
    }

    public async Task<bool> CreateSaveAsync(string name, MapExchangeStringData mapExchangeStringData, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        this.logger.LogDebug("Creating new save {name}", name);
        var savePath = this.options.Saves.GetSavePath(name);
        if (savePath.Exists)
        {
            if (!overwrite)
            {
                this.logger.LogWarning("Save file {path} already exists, not overwriting", savePath.FullName);
                return await exit(false);
            }

            this.logger.LogInformation("Save file {path} already exists, overwriting", savePath.FullName);
        }

        this.logger.LogDebug("Waiting for factorio process to exit");
        await this.StopAsync(cancellationToken);

        var factorioDirectory = await GetFactorioDirectoryAsync(cancellationToken);
        if (factorioDirectory == null)
        {
            this.logger.LogWarning("Unable to find Factorio directory");
            return await exit(false);
        }

        this.logger.LogDebug($"Writing map-settings.json and map-gen-settings.json");

        var mapSettingsPath = Path.GetTempFileName();
        var mapGenSettingsPath = Path.GetTempFileName();

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DictionaryKeyPolicy = JsonNamingPolicies.KebabCaseLower,
            PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower,
            NumberHandling = JsonNumberHandling.Strict,
        };

        using var mapSettingsStream = File.OpenWrite(mapSettingsPath);
        await JsonSerializer.SerializeAsync(mapSettingsStream, mapExchangeStringData.MapSettings, jsonOptions, cancellationToken);
        mapSettingsStream.WriteByte((byte)'\n');
        this.logger.LogDebug("Wrote map-settings.json to {path}", mapSettingsPath);

        using var mapGenSettingsStream = File.OpenWrite(mapGenSettingsPath);
        await JsonSerializer.SerializeAsync(mapGenSettingsStream, mapExchangeStringData.MapGenSettings, jsonOptions, cancellationToken);
        mapGenSettingsStream.WriteByte((byte)'\n');
        this.logger.LogDebug("Wrote map-gen-settings.json to {path}", mapGenSettingsPath);

        await Task.WhenAll(mapSettingsStream.FlushAsync(cancellationToken), mapGenSettingsStream.FlushAsync(cancellationToken));

        savePath = await CreateSaveAsync(factorioDirectory, savePath, new FileInfo(mapGenSettingsPath), new FileInfo(mapSettingsPath), mapExchangeStringData.MapGenSettings.Seed, cancellationToken);
        if (savePath == null)
        {
            this.logger.LogWarning("Failed to create save file");
            return await exit(false);
        }

        this.options.Saves.SetCurrentSavePath(savePath);
        this.logger.LogDebug("Created save file {path}", savePath.FullName);
        return await exit(true);

        async Task<bool> exit(bool success)
        {
            await this.StartAsync(cancellationToken);
            return success;
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
                this.options.Executable.GetRootDirectory(),
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
            // choose the save which was modified most recently
            savePath = this.options.Saves.ListSaves()
                .AsNullable()
                .FirstOrDefault()?.GetFileInfo();
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

        return await CreateSaveAsync(factorioDirectory, savePath, cancellationToken: cancellationToken);
    }

    private async Task<FileInfo?> CreateSaveAsync(
        DirectoryInfo factorioDirectory,
        FileInfo savePath,
        FileInfo? mapGenSettingsPath = null,
        FileInfo? mapSettingsPath = null,
        uint? seed = null,
        CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Creating save file {path}", savePath);

        var arguments = new List<string>()
        {
            "--create",
            savePath.FullName,
        };

        AddArgumentIfFileExists(arguments, "--map-gen-settings", mapGenSettingsPath);
        AddArgumentIfFileExists(arguments, "--map-settings", mapSettingsPath);
        if (seed.HasValue)
        {
            arguments.Add("--map-gen-seed");
            arguments.Add(seed.Value.ToString());
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

        var exitCode = await StartProcessWithOutputHandlersAndWaitForExitAsync(createSaveProcess, cancellationToken);
        if (exitCode != 0)
        {
            this.logger.LogError("Failed to create save file {path}", savePath);
            return null;
        }

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

    private async Task<int> StartProcessWithOutputHandlersAndWaitForExitAsync(Process process, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return -1;
        }

        process.OutputDataReceived += this.OnFactorioProcessOutputDataReceived;
        process.ErrorDataReceived += this.OnFactorioProcessErrorDataReceived;

        process.Start();

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // NOTE: when our parent process is shutting down, the process receives SIGTERM
            // however, if just cancellationToken is cancelled, no signal is sent to the underlying process
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (TaskCanceledException)
        {
        }

        if (!process.HasExited)
        {
            this.logger.LogDebug("Sending SIGINT to process {pid}", process.Id);
            Syscall.kill(process.Id, Signum.SIGINT);
        }

        async Task waitForExitWithSignalEscalation(Signum signum, string format, params object?[] args)
        {
            if (!process.HasExited)
            {
                this.logger.LogDebug(format, args);
                using var signalCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await process.WaitForExitAsync(signalCts.Token);
                }
                catch (TaskCanceledException)
                {
                    Syscall.kill(process.Id, Signum.SIGINT);
                }
            }
        }

        // System.Diagnostics.Process cancellation sometimes doesn't send signals to the underlying process when the cancellationToken is cancelled, so we have to do it ourselves
        await waitForExitWithSignalEscalation(Signum.SIGTERM, "SIGINT sent, waiting 5s for process {pid} to exit before escalating to SIGTERM", process.Id);
        await waitForExitWithSignalEscalation(Signum.SIGKILL, "SIGTERM sent, waiting 5s for process {pid} to exit before escalating to SIGKILL.", process.Id);

        if (!process.HasExited)
        {
            this.logger.LogWarning("Process did not exit after SIGKILL, immediately stopping it.");
            process.Kill();
        }

        process.CancelOutputRead();
        process.CancelErrorRead();

        return process.ExitCode;
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

        // empty banlist and adminlist files are required, otherwise adding players with in-game commands adds to a file in the executable directory
        // this file might get blown away by updates, so we make our own in the state directory
        var banlist = this.options.Configuration.GetServerBanlistPath();
        if (!banlist.Exists)
        {
            this.logger.LogDebug("No server banlist was found, creating an empty one.");
            using var banlistStream = banlist.OpenWrite();
            banlistStream.WriteByte((byte)'[');
            banlistStream.WriteByte((byte)']');
            banlistStream.Flush();
        }

        arguments.Add("--server-banlist");
        arguments.Add(banlist.FullName);

        var adminlist = this.options.Configuration.GetServerAdminlistPath();
        if (!adminlist.Exists)
        {
            this.logger.LogDebug("No server adminlist was found, creating an empty one.");
            using var adminlistStream = adminlist.OpenWrite();
            adminlistStream.WriteByte((byte)'[');
            adminlistStream.WriteByte((byte)']');
            adminlistStream.Flush();
        }

        arguments.Add("--server-adminlist");
        arguments.Add(adminlist.FullName);
    }

    private void AddModsArguments(List<string> arguments)
    {
        AddArgumentIfDirectoryExists(arguments, "--mod-directory", this.options.GetModsRootDirectory());
    }

    private static bool AddArgumentIfFileExists(List<string> arguments, string option, FileInfo? path)
    {
        if (path is null || !path.Exists)
        {
            return false;
        }

        arguments.Add(option);
        arguments.Add(path.FullName);
        return true;
    }

    private static bool AddArgumentIfDirectoryExists(List<string> arguments, string option, DirectoryInfo? path)
    {
        if (path is null || !path.Exists)
        {
            return false;
        }

        arguments.Add(option);
        arguments.Add(path.FullName);
        return true;
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
