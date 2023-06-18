using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Factoriod.Models.Mods;
using Microsoft.Extensions.Logging;
using Yoh.Text.Json.NamingPolicies;

namespace Factoriod.Fetcher;

public class ModFetcher
{
    private static readonly Uri BaseModsUri = new("https://mods.factorio.com");
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower,
    };

    private readonly ILogger<ModFetcher> logger;
    private readonly HttpClient httpClient;

    public ModFetcher(ILogger<ModFetcher> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task<bool> DownloadLatestAsync(Mod mod, FileInfo modListJson, DirectoryInfo downloadDirectory, FactorioAuthentication authentication, CancellationToken cancellationToken = default)
    {
        var latestModRelease = await GetLatestReleaseAsync(mod, authentication, cancellationToken);
        if (latestModRelease == null)
        {
            this.logger.LogWarning("Unable to find latest release for {mod}", mod);
            return false;
        }

        await DownloadAsync(mod, latestModRelease, downloadDirectory, modListJson, authentication, cancellationToken);
        return true;
    }

    /// <summary>
    /// Downloads all mods in <paramref name="mods"/> to <paramref name="downloadDirectory"/> and updates <paramref name="modListJson"/>.
    /// </summary>
    /// <param name="mods">The mods to download.</param>
    /// <param name="modListJson">The file to update with the new mods.</param>
    /// <param name="downloadDirectory">The directory to download to.</param>
    /// <param name="authentication">Authentication parameters for downloading.</param>
    /// <param name="cancellationToken">A token to cancel the download operation.</param>
    /// <returns>A task that completes when all <paramref name="mods"/> have been downloaded and <paramref name="modListJson"/> has been updated.</returns>
    public async Task BatchDownloadLatestAsync(IEnumerable<Mod> mods, FileInfo modListJson, DirectoryInfo downloadDirectory, FactorioAuthentication authentication, CancellationToken cancellationToken = default)
    {
        // TODO: limit batch size and concurrency
        var batch = mods.Select(downloadLatestAsync).ToList();
        var modReleases = await Task.WhenAll(batch);
        await UpdateModListAsync(modReleases, modListJson, cancellationToken);

        async Task<(Mod mod, ModRelease modRelease)> downloadLatestAsync(Mod mod)
        {
            var latestRelease = await GetLatestReleaseAsync(mod, authentication, cancellationToken)
                ?? throw new InvalidOperationException($"Unable to find latest release for {mod}");

            await DownloadModAsync(latestRelease, downloadDirectory, authentication, cancellationToken);
            return (mod, latestRelease);
        }
    }

    /// <summary>
    /// Downloads the latest release of a mod and updates the mod-list.json file.
    /// </summary>
    /// <param name="mod">The mod to download.</param>
    /// <param name="modRelease">The mod release to download.</param>
    /// <param name="downloadDirectory">The directory to download the mod to.</param>
    /// <param name="modListJson">The path to the mod-list.json file.</param>
    /// <param name="authentication">The user's authentication parameters to download the mod with.</param>
    /// <param name="cancellationToken">A token to cancel the download.</param>
    /// <returns>A task that completes when the mod has been downloaded.</returns>
    public async Task DownloadAsync(Mod mod, ModRelease modRelease, DirectoryInfo downloadDirectory, FileInfo modListJson, FactorioAuthentication authentication, CancellationToken cancellationToken = default)
    {
        await DownloadModAsync(modRelease, downloadDirectory, authentication, cancellationToken);
        await UpdateModListAsync(mod, modRelease, modListJson, cancellationToken);
    }


    private static Task UpdateModListAsync(Mod mod, ModRelease modRelease, FileInfo modListJson, CancellationToken cancellationToken)
        => UpdateModListAsync(new[] { (mod, modRelease) }, modListJson, cancellationToken);

    /// <summary>
    /// Updates <paramref name="modListJson"/> with information about <paramref name="mods"/>.
    /// </summary>
    /// <param name="mods">The mods to update.</param>
    /// <param name="modListJson">The file to update.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when <paramref name="modListJson"/> is updated.</returns>
    private static async Task UpdateModListAsync(IReadOnlyCollection<(Mod mod, ModRelease modRelease)> mods, FileInfo modListJson, CancellationToken cancellationToken)
    {
        List<ModListMod> modListMods;
        if (modListJson.Exists)
        {
            using var modListJsonFileStream = modListJson.OpenRead();
            var modList = await JsonSerializer.DeserializeAsync<ModList>(modListJsonFileStream, JsonSerializerOptions, cancellationToken);
            modListMods = modList?.Mods.ToList() ?? new List<ModListMod>();
        }
        else
        {
            modListMods = new List<ModListMod>();
        }

        var modsToUpdate = mods.Select(mod => new ModListMod(mod.mod.Name, true, mod.modRelease.Version)).ToHashSet();
        modListMods.RemoveAll(modListMod => modsToUpdate.Contains(modListMod));
        modListMods.AddRange(modsToUpdate);
        modListMods.Sort();

        // create or overwrite
        using (var modListJsonFileStream = modListJson.Open(FileMode.Create))
        {
            await JsonSerializer.SerializeAsync(modListJsonFileStream, new ModList(modListMods), JsonSerializerOptions, cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<ModRelease>?> ListReleasesAsync(Mod mod, FactorioAuthentication authentication, CancellationToken cancellationToken)
    {
        var shortModResultsUrl = CreateModResultUrl(mod, authentication);
        var modWithRelease = await httpClient.GetFromJsonAsync<Mod>(shortModResultsUrl, JsonSerializerOptions, cancellationToken);
        return modWithRelease?.Releases;
    }

    private async Task<ModRelease?> GetLatestReleaseAsync(Mod mod, FactorioAuthentication authentication, CancellationToken cancellationToken)
    {
        var modReleases = await ListReleasesAsync(mod, authentication, cancellationToken);
        if (modReleases == null)
        {
            this.logger.LogDebug("Unable to list releases for {mod}", mod);
            return null;
        }

        if (modReleases.Count == 0)
        {
            this.logger.LogDebug("Did not find any releases for {mod}", mod);
            return null;
        }

        var latestModRelease = modReleases.MaxBy(modRelease => modRelease.ReleasedAt);
        if (latestModRelease == null)
        {
            this.logger.LogDebug("Unable to find latest release for {mod} with {count} releases", mod, modReleases.Count);
            return null;
        }

        return latestModRelease;
    }

    /// <summary>
    /// Downloads a mod to a directory.
    /// </summary>
    /// <param name="modRelease">The mod release to download.</param>
    /// <param name="downloadDirectory">The directory to download the mod to.</param>
    /// <param name="authentication">The user's authentication parameters to download the mod with.</param>
    /// <param name="cancellationToken">A token to cancel the download.</param>
    /// <returns>A task taht completes when the mod has been downloaded.</returns>
    private async Task DownloadModAsync(ModRelease modRelease, DirectoryInfo downloadDirectory, FactorioAuthentication authentication, CancellationToken cancellationToken)
    {
        this.logger.LogDebug("Downloading {modRelease} to {directory}", modRelease, downloadDirectory.FullName);
        downloadDirectory.Create();
        var fullDownloadUrl = CreateDownloadUrl(modRelease, authentication);

        using var response = await httpClient.GetAsync(fullDownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var requestStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var destinationFile = Path.Combine(downloadDirectory.FullName, modRelease.FileName);
        using var outputFileStream = File.Open(destinationFile, FileMode.Create);
        await requestStream.CopyToAsync(outputFileStream, cancellationToken);
        response.Content = null;
    }

    private static Uri CreateModResultUrl(Mod mod, FactorioAuthentication authentication, bool full = false)
    {
        var builder = new UriBuilder(BaseModsUri)
        {
            Path = full ? mod.FullInformationUrl : mod.ShortInformationUrl,
            Query = authentication.QueryParameters,
        };

        return builder.Uri;
    }

    private static Uri CreateDownloadUrl(ModRelease modRelease, FactorioAuthentication authentication)
    {
        var builder = new UriBuilder(BaseModsUri)
        {
            Path = modRelease.DownloadUrl,
            Query = authentication.QueryParameters,
        };

        return builder.Uri;
    }
}
