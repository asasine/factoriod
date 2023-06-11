using Factoriod.Utilities;

namespace Factoriod.Models.Mods;

/// <summary>
/// A Factorio mod.
/// </summary>
/// <param name="Name">The name of the mod.</param>
/// <param name="Releases">The mod's releases.</param>
public record Mod(string Name, IReadOnlyCollection<ModRelease>? Releases = null)
{
    /// <summary>
    /// The mod's releases.
    /// </summary>
    public IReadOnlyCollection<ModRelease>? Release { get; } = Releases == null ? null : new PrintableReadOnlyCollection<ModRelease>(Releases);

    public string ShortInformationUrl => $"/api/mods/{Name}";

    public string FullInformationUrl => $"{ShortInformationUrl}/full";
}
