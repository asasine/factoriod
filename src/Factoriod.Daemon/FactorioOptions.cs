using Factoriod.Utilities;

namespace Factoriod.Daemon.Options
{
    public sealed class Factorio
    {
        public FactorioExecutable Executable { get; set; } = null!;
        public FactorioConfiguration Configuration { get; set; } = null!;
        public FactorioMapGeneration MapGeneration { get; set; } = null!;
        public string ModsRootDirectory { get; set; } = null!;

        public DirectoryInfo GetModsRootDirectory()
            => new DirectoryInfo(this.ModsRootDirectory).Resolve();
    }

    public sealed class FactorioExecutable
    {
        public string DownloadDirectory { get; set; } = null!;
        public string ExecutableDirectory { get; set; } = null!;
        public string ExecutableName { get; set; } = null!;

        public string Version { get; set; } = null!;
        public bool UseExperimental { get; set; }

        public DirectoryInfo GetDownloadDirectory()
            => new DirectoryInfo(this.DownloadDirectory).Resolve();

        public FileInfo GetExecutablePath(DirectoryInfo rootDirectory)
            => new FileInfo(Path.Combine(rootDirectory.FullName, this.ExecutableDirectory, this.ExecutableName)).Resolve();
    }

    public sealed class FactorioConfiguration
    {
        public string RootDirectory { get; set; } = null!;
        public string SavesDirectory { get; set; } = null!;
        public string Save { get; set; } = null!;
        public string ServerSettingsPath { get; set; } = null!;
        public string ServerWhitelistPath { get; set; } = null!;
        public string ServerBanlistPath { get; set; } = null!;
        public string ServerAdminlistPath { get; set; } = null!;

        public FileInfo GetSavePath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.SavesDirectory, this.Save)).Resolve();

        public FileInfo GetServerSettingsPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerSettingsPath)).Resolve();

        public FileInfo GetServerWhitelistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerWhitelistPath)).Resolve();

        public FileInfo GetServerBanlistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerBanlistPath)).Resolve();
        
        public FileInfo GetServerAdminlistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerAdminlistPath)).Resolve();
    }

    public sealed class FactorioMapGeneration
    {
        public string RootDirectory { get; set; } = null!;
        public string MapGenSettingsPath { get; set; } = null!;
        public int? MapGenSeed { get; set; }
        public string MapSettingsPath { get; set; } = null!;

        public FileInfo GetMapGenSettingsPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.MapGenSettingsPath)).Resolve();

        public FileInfo GetMapSettingsPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.MapSettingsPath)).Resolve();
    }
}
