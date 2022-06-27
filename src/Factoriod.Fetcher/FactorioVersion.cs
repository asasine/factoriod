using System;

namespace Factoriod.Fetcher
{
    public record FactorioVersion(Version Version, ReleaseBuild Build, bool Stable = true);
}
