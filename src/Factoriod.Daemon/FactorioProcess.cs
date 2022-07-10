using System.Diagnostics;
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

    public async Task<FactorioVersion> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult<FactorioVersion>(null);
    }

    public async Task<int> StartServerAsync(DirectoryInfo factorioDirectory, CancellationToken cancellationToken = default)
    {

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

        AddArgumentIfFileExists(arguments, "--server-settings", this.options.Configuration.GetServerSettingsPath());
        var addedWhitelist = AddArgumentIfFileExists(arguments, "--server-whitelist", this.options.Configuration.GetServerWhitelistPath());
        if (addedWhitelist)
        {
            arguments.Add("--use-server-whitelist");
        }

        AddArgumentIfFileExists(arguments, "--server-banlist", this.options.Configuration.GetServerBanlistPath());
        AddArgumentIfFileExists(arguments, "--server-adminlist", this.options.Configuration.GetServerAdminlistPath());
        AddArgumentIfDirectoryExists(arguments, "--mod-directory", this.options.GetModsRootDirectory());

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
