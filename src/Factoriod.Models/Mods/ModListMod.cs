﻿namespace Factoriod.Models.Mods;

/// <summary>
/// An entry in a <see cref="ModList"/>.
/// </summary>
/// <param name="Name">The name of the mod.</param>
/// <param name="Enabled">Whether the mod is enabled or not.</param>
public sealed record ModListMod(string Name, bool Enabled = true, string? Version = null) : IComparable<ModListMod>
{
    public override int GetHashCode() => Name.GetHashCode();

    public bool Equals(ModListMod? other) => other != null && other.Name == Name;

    public int CompareTo(ModListMod? other) => StringComparer.OrdinalIgnoreCase.Compare(Name, other?.Name);
}

public static class ModListModLinqExtensions
{
    public static IEnumerable<ModListMod> WithDisabled(this IEnumerable<ModListMod> source, params string[] modNames)
    {
        var modsToDisable = new HashSet<string>(modNames);
        return source.Select(mod => !modsToDisable.Contains(mod.Name) ? mod : mod with { Enabled = false, Version = null });
    }
}