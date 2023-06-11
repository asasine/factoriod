﻿using System.Text.Json;
using Factoriod.Utilities;
using Yoh.Text.Json.NamingPolicies;

namespace Factoriod.Models.Mods;

/// <summary>
/// The contents of a mod-list.json file.
/// </summary>
/// <param name="Mods">The mods.</param>
public record ModList(IReadOnlyCollection<ModListMod>? Mods = null)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower,
    };

    /// <summary>
    /// The mods.
    /// </summary>
    public IReadOnlyCollection<ModListMod> Mods { get; } =  new PrintableReadOnlyCollection<ModListMod>(Mods ?? new ModListMod[] { new ModListMod("base", true) });

    public async Task SerializeToAsync(FileInfo destination, CancellationToken cancellationToken = default)
    {
        using var stream = destination.OpenWrite();
        await JsonSerializer.SerializeAsync(stream, this, JsonSerializerOptions, cancellationToken);
    }

    public static async Task<ModList?> DeserialzeFromAsync(FileInfo source, CancellationToken cancellationToken = default)
    {
        if (!source.Exists)
        {
            return null;
        }

        using var stream = source.OpenRead();
        return await JsonSerializer.DeserializeAsync<ModList>(stream, JsonSerializerOptions, cancellationToken);
    }
}
