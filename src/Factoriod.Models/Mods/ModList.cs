using System.Text.Json;
using System.Text.Json.Serialization;
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
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// The mods.
    /// </summary>
    public IReadOnlyCollection<ModListMod> Mods { get; } =  new PrintableReadOnlyCollection<ModListMod>(Mods ?? new ModListMod[] { new ModListMod("base", true) });

    public async Task SerializeToAsync(FileInfo destination, CancellationToken cancellationToken = default)
    {
        // always overwrite the entire file's contents
        using var stream = destination.Open(FileMode.Create);
        await JsonSerializer.SerializeAsync(stream, this, JsonSerializerOptions, cancellationToken);
    }

    public static async Task<ModList?> DeserializeFromAsync(FileInfo source, CancellationToken cancellationToken = default)
    {
        if (!source.Exists)
        {
            return null;
        }

        if (source.Length == 0)
        {
            return new ModList();
        }

        using var stream = source.OpenRead();
        return await JsonSerializer.DeserializeAsync<ModList>(stream, JsonSerializerOptions, cancellationToken);
    }

    public ModList WithDisabled(params string[] names) => new(Mods.WithDisabled(names).ToList());
}
