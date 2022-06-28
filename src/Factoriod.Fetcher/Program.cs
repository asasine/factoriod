using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Factoriod.Fetcher
{
    public class Program
    {
        public static async Task Main()
        {
            using var client = new HttpClient();
            var versionFetcher = new VersionFetcher(client);
            var version = await versionFetcher.GetLatestHeadlessVersionAsync();
            if (version == null)
            {
                Console.WriteLine("No version found");
                return;
            }
            
            
            var outputDirectory = new DirectoryInfo(Path.Join(Path.GetTempPath(), "factoriod"));
            var downloadsDirectory = new DirectoryInfo(Path.Join(outputDirectory.FullName, "downloads"));
            outputDirectory.Create();
            Console.WriteLine($"Downloading {version} to {downloadsDirectory}");
            var releaseFetcher = new ReleaseFetcher(client);
            var extractedDirectory = await releaseFetcher.DownloadToAsync(version, Distro.Linux64, downloadsDirectory);
            Console.WriteLine($"Downloaded and extracted to {extractedDirectory}");
        }
    }
}
