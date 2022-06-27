using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Readers;

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
            var outputFile = await releaseFetcher.DownloadToAsync(version, Distro.Linux64, downloadsDirectory);
            Console.WriteLine($"Downloaded to {outputFile}");

            if (outputFile.Directory == null)
            {
                Console.WriteLine($"Could not find directory of {outputFile}");
                return;
            }

            var extractedFiles = Extract(outputFile, outputFile.Directory);
            Console.WriteLine($"Extracted to {extractedFiles}");
        }

        private static DirectoryInfo Extract(FileInfo input, DirectoryInfo output)
        {
            Console.WriteLine($"Extracting {input} to {output}");
            output.Create();
            using var inputStream = input.OpenRead();
            using var reader = ReaderFactory.Open(inputStream);
            reader.WriteAllToDirectory(output.FullName, new ExtractionOptions
            {
                ExtractFullPath = true,
                Overwrite = true
            });

            return new DirectoryInfo(Path.Join(output.FullName, "factorio"));
        }
    }
}
