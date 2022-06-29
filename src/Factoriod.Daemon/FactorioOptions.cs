namespace Factoriod.Daemon.Options
{
    public sealed class Factorio
    {
        public FactorioExecutable Executable { get; set; } = null!;
        public FactorioConfiguration Configuration { get; set; } = null!;
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
        public string ServerSettingsPath { get; set; } = null!;
        public string ServerWhitelistPath { get; set; } = null!;
        public string ServerBanlistPath { get; set; } = null!;
        public string ServerAdminlistPath { get; set; } = null!;
    }
}
