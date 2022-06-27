using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Factoriod.Fetcher
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using var client = new HttpClient();
            var outputFile = Path.GetTempFileName();
            var versionFetcher = new VersionFetcher(client);
            var versions = versionFetcher.GetVersionsAsync();
            if (versions == null)
            {
                Console.WriteLine("No versions found");
                return;
            }

            await foreach (var version in versions)
            {
                Console.WriteLine(version);
            }
        }
    }
}
