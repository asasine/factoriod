using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace Factoriod.Fetcher
{
    public class ReleaseFetcher
    {
        private readonly HttpClient client;

        public ReleaseFetcher(HttpClient client)
        {
            this.client = client;
        }

        public async Task<DirectoryInfo?> DownloadToAsync(FactorioVersion version, Distro distro, DirectoryInfo outputDirectory, CancellationToken cancellationToken = default)
        {
            var versionedOutputDirectory = Path.Join(outputDirectory.FullName, version.Version.ToString(), version.Build.ToString(), Distro.Linux64.ToString());
            Directory.CreateDirectory(versionedOutputDirectory);
            var outputFile = new FileInfo(Path.Join(versionedOutputDirectory, "factorio.tar.xz"));

            Console.WriteLine($"Downloading to {outputDirectory}");

            if (!outputFile.Exists)
            {
                Console.WriteLine($"File does not exists, downloading");
                var downloadUrl = GetDownloadUrl(version, distro);
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();

                using var requestStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var outputFileStream = File.Open(outputFile.FullName, FileMode.Create);
                await requestStream.CopyToAsync(outputFileStream, cancellationToken);
                response.Content = null;
            }

            if (outputFile.Directory == null)
            {
                Console.WriteLine($"Could not find directory of {outputFile}");
                return null;
            }

            return Extract(outputFile, outputFile.Directory);
        }

        private static string GetDownloadUrl(FactorioVersion version, Distro distro)
            => $"https://factorio.com/get-download/{version.Version}/{version.Build}/{distro}";

            
        private static DirectoryInfo Extract(FileInfo input, DirectoryInfo output)
        {
            Console.WriteLine($"Extracting {input} to {output}");
            output.Create();
            using var inputStream = input.OpenRead();
            using var reader = ReaderFactory.Open(inputStream);
            reader.WriteAllToDirectory(output.FullName, new ExtractionOptions
            {
                ExtractFullPath = true,
                Overwrite = true,

                // throws NotImplementedException, need another way to preserve file permissions
                // PreserveAttributes = true,
            });

            return new DirectoryInfo(Path.Join(output.FullName, "factorio"));
        }
    }
}
