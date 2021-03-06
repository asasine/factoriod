using System;
using System.IO;

namespace Factoriod.Models;

public record FactorioVersion(Version Version, ReleaseBuild Build, bool Stable = true)
{
    public string GetRelativePath(Distro distro) => Path.Combine(Version.ToString(), Build.ToString(), distro.ToString());
}
