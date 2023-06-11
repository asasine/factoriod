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
        var modReleases = await ListReleasesAsync(mod, authentication, cancellationToken);
        if (modReleases == null)
        {
            this.logger.LogInformation("Unable to list releases for {mod}", mod);
            return false;
        }

        if (modReleases.Count == 0)
        {
            this.logger.LogInformation("Did not find any releases for {mod}", mod);
            return false;
        }

        var latestModRelease = modReleases.MaxBy(modRelease => modRelease.ReleasedAt);
        if (latestModRelease == null)
        {
            this.logger.LogInformation("Unable to find latest release for {mod} with {count} releases", mod, modReleases.Count);
            return false;
        }

        await DownloadAsync(mod, latestModRelease, downloadDirectory, modListJson, authentication, cancellationToken);
        return true;
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
        this.logger.LogDebug("Downloading {modRelease} to {directory} and updating {file}", modRelease, downloadDirectory.FullName, modListJson.FullName);
        await DownloadModAsync(modRelease, downloadDirectory, authentication, cancellationToken);

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

        modListMods.RemoveAll(modListMod => modListMod.Name == mod.Name);
        modListMods.Add(new ModListMod(mod.Name, true, modRelease.Version));
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
