namespace Factoriod.Models;

public record struct FactorioDirectory(FactorioVersion Version, Distro Distro, DirectoryInfo Path)
{
    public static implicit operator (FactorioVersion version, Distro distro, DirectoryInfo path)(FactorioDirectory value) => (value.Version, value.Distro, value.Path);

    public static implicit operator FactorioDirectory((FactorioVersion version, Distro distro, DirectoryInfo path) value) => new FactorioDirectory(value.version, value.distro, value.path);
}
