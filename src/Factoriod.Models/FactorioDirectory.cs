namespace Factoriod.Models;

public record struct FactorioDirectory(FactorioVersion Version, Distro Distro, DirectoryInfo Path);
