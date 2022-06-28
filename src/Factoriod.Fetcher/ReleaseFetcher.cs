using System;
using System.Diagnostics;
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

            return await ExtractAsync(outputFile, outputFile.Directory, cancellationToken);
        }

        private static string GetDownloadUrl(FactorioVersion version, Distro distro)
            => $"https://factorio.com/get-download/{version.Version}/{version.Build}/{distro}";

            
        private static async Task<DirectoryInfo?> ExtractAsync(FileInfo input, DirectoryInfo output, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"Extracting {input} to {output}");
            output.Create();

            using var tarProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xJf {input.FullName} -C {output.FullName}",
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });

            if (tarProcess == null)
            {
                Console.WriteLine("Could not start tar process");
                return null;
            }

            await tarProcess.WaitForExitAsync(cancellationToken);
            if (tarProcess.ExitCode != 0)
            {
                Console.WriteLine($"Tar process exited with code {tarProcess.ExitCode}");
                Console.WriteLine($"stdout:\n{tarProcess.StandardOutput.ReadToEnd()}");
                Console.WriteLine($"stderr:\n{tarProcess.StandardError.ReadToEnd()}");
                return null;
            }

            return new DirectoryInfo(Path.Join(output.FullName, "factorio"));
        }
    }
}
