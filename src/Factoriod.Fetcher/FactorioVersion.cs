using System;

namespace Factoriod.Fetcher
{
    public record FactorioVersion(Version Version, ReleaseBuild Build, bool Stable = true);

    public enum ReleaseBuild
    {
        Alpha,
        Demo,
        Headless,
    }

    public enum Distro
    {
        Win64,
        Win64Manual,
        Win32,
        Win32Manual,
        OSX,
        Linux64,
        Linux32,
    }
}
