namespace Factoriod.Models;

public readonly record struct FactorioDirectory(FactorioVersion Version, Distro Distro, DirectoryInfo Path);
