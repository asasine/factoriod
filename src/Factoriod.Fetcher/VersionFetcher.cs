using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
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

        public async Task<IEnumerable<FactorioDirectory>?> GetVersionsOnDiskAsync(DirectoryInfo directory, CancellationToken cancellationToken = default)
        {
            directory = directory.Resolve();
            var versions = GetVersionsAsync(false, cancellationToken);
            IReadOnlyDictionary<ReleaseBuild, Version> latestStableVersions;
            if (versions == null)
            {
                latestStableVersions = new Dictionary<ReleaseBuild, Version>();
            }
            else
            {
                latestStableVersions = await versions.ToDictionaryAsync(factorioVersion => factorioVersion.Build, factorioVersion => factorioVersion.Version, cancellationToken);
            }

            return GetVersionsOnDisk(directory, latestStableVersions);
        }

        public IEnumerable<FactorioDirectory> GetVersionsOnDisk(DirectoryInfo directory, IReadOnlyDictionary<ReleaseBuild, Version> latestStableVersions)
        {
            directory = directory.Resolve();
            this.logger.LogDebug("Scanning {directory}", directory.FullName);
            if (!directory.Exists)
            {
                yield break;
            }

            foreach (var versionDirectory in directory.EnumerateDirectories())
            {
                var version = Version.Parse(versionDirectory.Name);
                foreach (var buildDirectory in versionDirectory.EnumerateDirectories())
                {
                    var releaseBuild = new ReleaseBuild(buildDirectory.Name);
                    var stable = latestStableVersions.TryGetValue(releaseBuild, out var latestStable) && version <= latestStable;
                    var factorioVersion = new FactorioVersion(version, releaseBuild, stable);
                    foreach (var distroDirectory in buildDirectory.EnumerateDirectories())
                    {
                        if (Distro.TryParse(distroDirectory.Name, out var distro))
                        {
                            var factorioDirectory = new DirectoryInfo(Path.Combine(distroDirectory.FullName, "factorio")).Resolve();
                            yield return (factorioVersion, distro, factorioDirectory);
                        }
                    }
                }
            }
        }
    }
}
