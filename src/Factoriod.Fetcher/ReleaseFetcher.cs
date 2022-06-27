using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Factoriod.Fetcher
{
    public class ReleaseFetcher
    {
        private readonly HttpClient client;

        public ReleaseFetcher(HttpClient client)
        {
            this.client = client;
        }

        public async Task<FileInfo> DownloadToAsync(FactorioVersion version, Distro distro, DirectoryInfo outputDirectory, CancellationToken cancellationToken = default)
        {
            var versionedOutputDirectory = Path.Join(outputDirectory.FullName, version.Version.ToString(), version.Build.ToString(), Distro.Linux64.ToString());
            Directory.CreateDirectory(versionedOutputDirectory);
            var outputFile = new FileInfo(Path.Join(versionedOutputDirectory, "factorio.tar.xz"));
            Console.WriteLine($"Downloading to {outputDirectory}");

            if (outputFile.Exists)
            {
                Console.WriteLine($"File already exists: {outputFile}");
                return outputFile;
            }

            var downloadUrl = GetDownloadUrl(version, distro);
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var requestStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var outputFileStream = File.Open(outputFile.FullName, FileMode.Create);
            await requestStream.CopyToAsync(outputFileStream, cancellationToken);
            response.Content = null;

            return outputFile;
        }

        private static string GetDownloadUrl(FactorioVersion version, Distro distro)
            => $"https://factorio.com/get-download/{version.Version}/{version.Build}/{distro}";
    }
}
