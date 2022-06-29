namespace Factoriod.Daemon.Options
{
    public sealed class Factorio
    {
        public FactorioExecutable Executable { get; set; } = null!;
        public FactorioConfiguration Configuration { get; set; } = null!;
        public string ModsRootDirectory { get; set; } = null!;
    }

    public sealed class FactorioExecutable
    {
        public string RootDirectory { get; set; } = null!;
        public string ExecutableName { get; set; } = null!;
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

        public string GetSavePath()
            => PathUtilities.ResolveTilde(Path.Join(this.RootDirectory, this.SavesDirectory, this.Save));

        public string GetServerSettingsPath()
            => PathUtilities.ResolveTilde(Path.Join(this.RootDirectory, this.ServerSettingsPath));

        public string GetServerWhitelistPath()
            => PathUtilities.ResolveTilde(Path.Join(this.RootDirectory, this.ServerWhitelistPath));

        public string GetServerBanlistPath()
            => PathUtilities.ResolveTilde(Path.Join(this.RootDirectory, this.ServerBanlistPath));
        
        public string GetServerAdminlistPath()
            => PathUtilities.ResolveTilde(Path.Join(this.RootDirectory, this.ServerAdminlistPath));
    }
}
