using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Factoriod.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Factoriod.Fetcher
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var includeExperimental = args.Length > 0 && args[0] == "--include-experimental";

            using var client = new HttpClient();
            var versionFetcher = new VersionFetcher(NullLogger<VersionFetcher>.Instance, client);
            var version = await versionFetcher.GetLatestHeadlessVersionAsync(includeExperimental);
            if (version == null)
            {
                Console.WriteLine("No version found");
                return;
            }
            
            
            var outputDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "factoriod"));
            var downloadsDirectory = new DirectoryInfo(Path.Combine(outputDirectory.FullName, "downloads"));
            outputDirectory.Create();
            Console.WriteLine($"Downloading {version} to {downloadsDirectory}");
            var releaseFetcher = new ReleaseFetcher(NullLogger<ReleaseFetcher>.Instance, client);
            var extractedDirectory = await releaseFetcher.DownloadToAsync(version, Distro.Linux64, downloadsDirectory);
            Console.WriteLine($"Downloaded and extracted to {extractedDirectory}");
        }
    }
}
