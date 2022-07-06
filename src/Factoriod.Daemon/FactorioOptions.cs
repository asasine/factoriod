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
            => new DirectoryInfo(this.ModsRootDirectory).ResolveTilde();
    }

    public sealed class FactorioExecutable
    {
        public string DownloadDirectory { get; set; } = null!;
        public string ExecutableDirectory { get; set; } = null!;
        public string ExecutableName { get; set; } = null!;

        public string Version { get; set; } = null!;
        public bool UseExperimental { get; set; }

        public DirectoryInfo GetDownloadDirectory()
            => new(Path.GetFullPath(this.DownloadDirectory));

        public FileInfo GetExecutablePath(DirectoryInfo rootDirectory)
            => new FileInfo(Path.Combine(rootDirectory.FullName, this.ExecutableDirectory, this.ExecutableName)).ResolveTilde();
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
            => new FileInfo(Path.Combine(this.RootDirectory, this.SavesDirectory, this.Save)).ResolveTilde();

        public FileInfo GetServerSettingsPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerSettingsPath)).ResolveTilde();

        public FileInfo GetServerWhitelistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerWhitelistPath)).ResolveTilde();

        public FileInfo GetServerBanlistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerBanlistPath)).ResolveTilde();
        
        public FileInfo GetServerAdminlistPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.ServerAdminlistPath)).ResolveTilde();
    }

    public sealed class FactorioMapGeneration
    {
        public string RootDirectory { get; set; } = null!;
        public string MapGenSettingsPath { get; set; } = null!;
        public int? MapGenSeed { get; set; }
        public string MapSettingsPath { get; set; } = null!;

        public FileInfo GetMapGenSettingsPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.MapGenSettingsPath)).ResolveTilde();

        public FileInfo GetMapSettingsPath()
            => new FileInfo(Path.Combine(this.RootDirectory, this.MapSettingsPath)).ResolveTilde();
    }
}
