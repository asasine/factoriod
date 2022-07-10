using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Factoriod.Models;
using Factoriod.Utilities;
using Microsoft.Extensions.Logging;

namespace Factoriod.Fetcher
{
    public class ReleaseFetcher
    {
        private readonly ILogger logger;
        private readonly HttpClient client;

        public ReleaseFetcher(ILogger<ReleaseFetcher> logger, HttpClient client)
        {
            this.logger = logger;
            this.client = client;
        }

        public async Task<DirectoryInfo?> DownloadToAsync(FactorioVersion version, Distro distro, DirectoryInfo outputDirectory, CancellationToken cancellationToken = default)
        {
            outputDirectory.Create();
            var outputFile = new FileInfo(Path.Combine(outputDirectory.FullName, "factorio.tar.xz"));

            this.logger.LogDebug("Downloading to {outputDirectory}", outputDirectory);
            var downloadUrl = GetDownloadUrl(version, distro);
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var requestStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var outputFileStream = File.Open(outputFile.FullName, FileMode.Create);
            await requestStream.CopyToAsync(outputFileStream, cancellationToken);
            response.Content = null;

            if (outputFile.Directory == null)
            {
                this.logger.LogDebug("Could not find directory of {outputFile}", outputFile);
                return null;
            }

            var extractedDirectory = await ExtractAsync(outputFile, outputFile.Directory, cancellationToken);
            outputFile.Delete();
            return extractedDirectory;
        }

        private static string GetDownloadUrl(FactorioVersion version, Distro distro)
            => $"https://factorio.com/get-download/{version.Version}/{version.Build}/{distro}";

            
        private async Task<DirectoryInfo?> ExtractAsync(FileInfo input, DirectoryInfo output, CancellationToken cancellationToken = default)
        {
            this.logger.LogDebug("Extracting {input} to {output}", input, output);
            var factorioDirectory = new DirectoryInfo(Path.Combine(output.FullName, "factorio"));
            if (factorioDirectory.Exists)
            {
                factorioDirectory.Delete(true);
            }

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
                this.logger.LogWarning("Could not start tar process");
                return null;
            }

            await tarProcess.WaitForExitAsync(cancellationToken);
            if (tarProcess.ExitCode != 0)
            {
                this.logger.LogDebug("Tar process exited with code {exitCode}", tarProcess.ExitCode);
                this.logger.LogTrace("stdout:\n{stdout}", tarProcess.StandardOutput.ReadToEnd());
                this.logger.LogTrace("stderr:\n{stderr}", tarProcess.StandardError.ReadToEnd());
                return null;
            }

            return factorioDirectory;
        }
    }
}
