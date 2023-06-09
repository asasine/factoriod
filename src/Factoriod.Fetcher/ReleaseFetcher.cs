using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            if (outputFile.Directory == null)
            {
                this.logger.LogDebug("Output file directory does not exist, creating it");
                outputDirectory.Create();
                outputFile.Refresh();
                if (outputFile.Directory == null)
                {
                    this.logger.LogWarning("Output file directory is still null");
                    return null;
                }
            }

            this.logger.LogDebug("Downloading to {outputDirectory}", outputDirectory);
            var downloadUrl = GetDownloadUrl(version, distro);
            var success = await DownloadFileAsync(downloadUrl, outputFile, cancellationToken);
            if (!success)
            {
                this.logger.LogDebug("Failed to download {outputFile}", outputFile);
                return null;
            }

            var extractedDirectory = await ExtractAsync(outputFile, outputFile.Directory, cancellationToken);
            outputFile.Delete();
            return extractedDirectory;
        }

        public async Task<FactorioUpdate?> DownloadUpdateAsync(FromToVersion update, DirectoryInfo outputDirectory, CancellationToken cancellationToken = default)
        {
            outputDirectory.Create();
            this.logger.LogDebug("Downloading to {outputDirectory}", outputDirectory);
            var outputFile = new FileInfo(Path.Combine(outputDirectory.FullName, $"{update.From}-{update.To}-update.zip"));

            // calling the URL returns a list of URLs which are URLs of zip files
            var downloadUrl = GetUpdateDownloadUrl(update);
            using var response = await client.GetAsync(downloadUrl, cancellationToken);

            var updateUrls = await response.Content.ReadAsAsync<List<string>>(cancellationToken: cancellationToken);
            if (updateUrls == null)
            {
                this.logger.LogDebug("Failed to get a download URL for the update from {fromVersion} to {toVersion}", update.From, update.To);
                return null;
            }

            if (updateUrls.Count > 1)
            {
                this.logger.LogDebug("Found multiple download URLs for the update from {fromVersion} to {toVersion}, unsure how to proeed.", update.From, update.To);
                return null;
            }

            var updateUrl = updateUrls.Single();
            if (updateUrl == null)
            {
                this.logger.LogDebug("The download URL is null for the update from {fromVersion} to {toVersion}", update.From, update.To);
                return null;
            }

            var success = await DownloadFileAsync(updateUrl, outputFile, cancellationToken);
            if (!success)
            {
                this.logger.LogDebug("Failed to download the update from {fromVersion} to {toVersion}", update.From, update.To);
                return null;
            }

            return new FactorioUpdate(update, outputFile);
        }

        private async Task<bool> DownloadFileAsync(string url, FileInfo outputFile, CancellationToken cancellationToken = default)
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var requestStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var outputFileStream = File.Open(outputFile.FullName, FileMode.Create);
            await requestStream.CopyToAsync(outputFileStream, cancellationToken);
            response.Content = null;

            return outputFile.Directory != null;
        }

        private static string GetUpdateDownloadUrl(FromToVersion update)
            => $"https://updater.factorio.com/get-download-link?from={update.From}&to={update.To}&apiVersion=2&package=core-linux_headless64";

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
