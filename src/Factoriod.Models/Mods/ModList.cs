using Factoriod.Utilities;

namespace Factoriod.Models.Mods;

/// <summary>
/// The contents of a mod-list.json file.
/// </summary>
/// <param name="Mods">The mods.</param>
public record ModList(IReadOnlyCollection<ModListMod> Mods)
{
    /// <summary>
    /// The mods.
    /// </summary>
    public IReadOnlyCollection<ModListMod> Mods { get; } = new PrintableReadOnlyCollection<ModListMod>(Mods);
}
