using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Factoriod.Fetcher
{
    public class VersionFetcher
    {
        private readonly HttpClient client;

        private const string VersionUrl = "https://factorio.com/api/latest-releases";

        public VersionFetcher(HttpClient client)
        {
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

        public async Task<FactorioVersion?> GetLatestHeadlessVersionAsync(CancellationToken cancellationToken = default)
        {
            var versions = this.GetVersionsAsync(false, cancellationToken);
            if (versions == null)
            {
                return null;
            }

            return await versions.Where(version => version.Build == ReleaseBuild.Headless).FirstOrDefaultAsync(cancellationToken);
        }
    }
}
