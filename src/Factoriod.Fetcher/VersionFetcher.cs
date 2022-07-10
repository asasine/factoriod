using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Factoriod.Models;
using Factoriod.Utilities;
using Microsoft.Extensions.Logging;

namespace Factoriod.Fetcher
{
    public class VersionFetcher
    {
        private readonly ILogger logger;
        private readonly HttpClient client;

        private const string VersionUrl = "https://factorio.com/api/latest-releases";
        private const string UpdatesUrl = "https://updater.factorio.com/get-available-versions";

        public VersionFetcher(ILogger<VersionFetcher> logger, HttpClient client)
        {
            this.logger = logger;
            this.client = client;
        }

        public async IAsyncEnumerable<FactorioVersion>? GetVersionsAsync(bool includeExperimental = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var response = await this.client.GetAsync(VersionUrl, cancellationToken: cancellationToken);
            var versions = await response.Content.ReadAsAsync<Dictionary<string, Dictionary<string, string>>>(cancellationToken: cancellationToken);
            if (versions == null)
            {
                yield break;
            }

            foreach (var (release, releases) in versions)
            {
                var stable = release == "stable";
                if (!includeExperimental && !stable)
                {
                    continue;
                }

                foreach (var (build, version) in releases)
                {
                    yield return new FactorioVersion(
                        Version.Parse(version),
                        new ReleaseBuild(build),
                        stable);
                }
            }
        }

        public async Task<FactorioVersion?> GetLatestHeadlessVersionAsync(bool includeExperimental = false, CancellationToken cancellationToken = default)
        {
            var versions = this.GetVersionsAsync(includeExperimental, cancellationToken);
            if (versions == null)
            {
                return null;
            }

            return await versions.Where(version => version.Build == ReleaseBuild.Headless).FirstOrDefaultAsync(cancellationToken);
        }

        private record BaseInfoJson(Version Version);

        /// <summary>
        /// Gets the version of the factorio instance downloaded to <paramref name="directory"/>.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<FactorioDirectory?> GetVersionAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
        {
            directory = directory.Resolve();
            var versions = GetVersionsAsync(false, cancellationToken);
            Version? latestStableHeadlessVersion = null;
            if (versions == null)
            {
            }
            else
            {
                var latestStableHeadlessFactorioVersion = await versions.SingleOrDefaultAsync(factorioVersion => factorioVersion.Stable && factorioVersion.Build == ReleaseBuild.Headless, cancellationToken);
                latestStableHeadlessVersion = latestStableHeadlessFactorioVersion?.Version;
            }

            var baseInfoJsonFile = new FileInfo(Path.Combine(directory.FullName, "data", "base", "info.json"));
            if (!baseInfoJsonFile.Exists)
            {
                return null;
            }

            using var contents = baseInfoJsonFile.OpenRead();
            var baseInfo = await JsonSerializer.DeserializeAsync<BaseInfoJson>(contents,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                },
                cancellationToken);

            if (baseInfo == null)
            {
                return null;
            }

            var isStable = baseInfo.Version >= latestStableHeadlessVersion;
            return new FactorioDirectory(new FactorioVersion(baseInfo.Version, ReleaseBuild.Headless, isStable), Distro.Linux64, directory);
        }

        
        private record AvailableVersions([property: JsonPropertyName("core-linux_headless64")] List <FromToVersion> CoreLinuxHeadless64);

        public async Task<ILookup<Version, Version>> GetAvailableUpdatesAsync(CancellationToken cancellationToken = default)
        {
            using var response = await this.client.GetAsync(UpdatesUrl, cancellationToken);
            var availableVersions = await response.Content.ReadAsAsync<AvailableVersions>(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                },
                cancellationToken);

            availableVersions ??= new AvailableVersions(new List<FromToVersion>());
            return availableVersions.CoreLinuxHeadless64.ToLookup(fromTo => fromTo.From, fromTo => fromTo.To);
        }

        public async Task<IEnumerable<FromToVersion>?> GetUpdatePathAsync(Version from, Version to, CancellationToken cancellationToken = default)
        {
            var availableUpdates = await GetAvailableUpdatesAsync(cancellationToken);
            var updatePath = new List<FromToVersion>();

            for (var latestVersion = from; !latestVersion.Equals(to); latestVersion = updatePath.Last().To)
            {
                if (availableUpdates.Contains(latestVersion))
                {
                    var updatesFrom = availableUpdates[latestVersion].ToHashSet();
                    var bestUpdate = updatesFrom.Contains(to) ? to : updatesFrom.Max();
                    if (bestUpdate == null)
                    {
                        // no update path found
                        return null;
                    }

                    updatePath.Add(new FromToVersion(latestVersion, bestUpdate));
                }
                else
                {
                    // no update path found
                    return null;
                }
            }

            return updatePath;
        }
    }
}
