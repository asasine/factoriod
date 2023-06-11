using System.ComponentModel.DataAnnotations;
using Factoriod.Models;
using Factoriod.Utilities;

namespace Factoriod.Daemon.Options
{
    public sealed class Factorio
    {
        public FactorioExecutable Executable { get; set; } = null!;
        public FactorioConfiguration Configuration { get; set; } = null!;
        public FactorioSaves Saves { get; set; } = null!;
        public string ModsRootDirectory { get; set; } = null!;

        public DirectoryInfo GetModsRootDirectory()
            => new DirectoryInfo(this.ModsRootDirectory).Resolve();

        public FileInfo GetModListJsonFile()
            => new(Path.Combine(GetModsRootDirectory().FullName, "mod-list.json"));
    }

    public sealed class FactorioExecutable
    {
        [Required]
        public string RootDirectory { get; set; } = null!;
        public string ExecutableDirectory { get; set; } = null!;
        public string ExecutableName { get; set; } = null!;

        public string Version { get; set; } = null!;
        public bool UseExperimental { get; set; }

        public DirectoryInfo GetRootDirectory()
            => new DirectoryInfo(this.RootDirectory).Resolve();

        public DirectoryInfo GetFactorioDirectory()
            => new(Path.Combine(GetRootDirectory().FullName, "factorio"));

        public DirectoryInfo GetUpdatesDirectory()
            => new(Path.Combine(GetRootDirectory().FullName, "updates"));

        public FileInfo GetExecutablePath(DirectoryInfo rootDirectory)
            => new FileInfo(Path.Combine(rootDirectory.FullName, this.ExecutableDirectory, this.ExecutableName)).Resolve();
    }

    public sealed class FactorioConfiguration
    {
        [Required]
        public string RootDirectory { get; set; } = null!;
        public string ServerSettingsPath { get; set; } = null!;
        public string ServerWhitelistPath { get; set; } = null!;
        public string ServerBanlistPath { get; set; } = null!;
        public string ServerAdminlistPath { get; set; } = null!;

        public FileInfo GetServerSettingsPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerSettingsPath)).Resolve();

        public FileInfo GetServerWhitelistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerWhitelistPath)).Resolve();

        public FileInfo GetServerBanlistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerBanlistPath)).Resolve();

        public FileInfo GetServerAdminlistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerAdminlistPath)).Resolve();
    }

    public sealed class FactorioSaves
    {
        [Required]
        public string RootDirectory { get; set; } = null!;

        public DirectoryInfo GetRootDirectory()
            => new DirectoryInfo(this.RootDirectory).Resolve();

        private const string CurrentSaveFilename = "current_save";

        public FileInfo? GetCurrentSavePath()
        {
            var currentSaveLink = new FileInfo(Path.Combine(GetRootDirectory().FullName, CurrentSaveFilename));
            if (!currentSaveLink.Exists)
            {
                return null;
            }

            var resolved = currentSaveLink.ResolveLinkTarget(true);
            if (resolved == null)
            {
                return null;
            }

            if (!resolved.Exists)
            {
                return null;
            }

            var currentSave = new FileInfo(resolved.FullName);
            if (!currentSave.Exists)
            {
                return null;
            }

            return currentSave;
        }

        public void SetCurrentSavePath(FileInfo save)
        {
            var currentSaveLink = new FileInfo(Path.Combine(GetRootDirectory().FullName, CurrentSaveFilename));
            if (currentSaveLink.Exists)
            {
                currentSaveLink.Delete();
            }

            currentSaveLink.CreateAsSymbolicLink(save.FullName);
        }

        public FileInfo GetSavePath(string name)
            => new(Path.Combine(GetRootDirectory().FullName, $"{name}.zip"));

        /// <summary>
        /// List all saves in the saves directory, ordered by last write time descending.
        /// </summary>
        /// <returns>All saves in the saves directory, ordered by last write time descending.</returns>
        public IEnumerable<Save> ListSaves()
        {
            var savesRootDirectory = GetRootDirectory();
            savesRootDirectory.Create();
            return savesRootDirectory
                .EnumerateFiles("*.zip")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Select(file => new Save(file.FullName));
        }
    }
}
