using System.ComponentModel.DataAnnotations;
using Factoriod.Utilities;

namespace Factoriod.Models.Game;

public record MapGenSettings(
    [Range(0, 6)]
    float TerrainSegmentation = 1,

    [Range(0, 6)]
    float Water = 1,
    IReadOnlyDictionary<string, AutoplaceControl>? AutoplaceControls = null,
    IReadOnlyDictionary<string, AutoplaceSettings>? AutoplaceSettings = null,
    CliffPlacementSettings? CliffSettings = null,
    uint? Seed = null,

    [Range(0, 2000000)]
    uint Width = 0,

    [Range(0, 2000000)]
    uint Height = 0,

    [Range(0, 6)]
    float StartingArea = 1,
    IReadOnlyCollection<MapPosition>? StartingPoints = null,
    bool PeacefulMode = false,
    IReadOnlyDictionary<string, string>? PropertyExpressionNames = null
)
{
    private static readonly IReadOnlyDictionary<string, AutoplaceControl> defaultAutoplaceControls = new Dictionary<string, AutoplaceControl>
    {
        { "coal", new AutoplaceControl(1, 1, 1) },
        { "copper-ore", new AutoplaceControl(1, 1, 1) },
        { "crude-oil", new AutoplaceControl(1, 1, 1) },
        { "enemy-base", new AutoplaceControl(1, 1, 1) },
        { "iron-ore", new AutoplaceControl(1, 1, 1) },
        { "stone", new AutoplaceControl(1, 1, 1) },
        { "trees", new AutoplaceControl(1, 1, 1) },
        { "uranium-ore", new AutoplaceControl(1, 1, 1) },
    };

    public IReadOnlyDictionary<string, AutoplaceControl> AutoplaceControls { get; } = new PrintableReadOnlyDictionary<string, AutoplaceControl>(AutoplaceControls?.WithDefaults(defaultAutoplaceControls) ?? defaultAutoplaceControls);
    public IReadOnlyDictionary<string, AutoplaceSettings> AutoplaceSettings { get; } = AutoplaceSettings ?? new PrintableReadOnlyDictionary<string, AutoplaceSettings>();
    public CliffPlacementSettings CliffSettings { get; } = CliffSettings ?? new();
    public IReadOnlyCollection<MapPosition> StartingPoints { get; } = StartingPoints ?? new PrintableReadOnlyCollection<MapPosition>(new MapPosition(0, 0));
    public IReadOnlyDictionary<string, string> PropertyExpressionNames { get; } = PropertyExpressionNames ?? new PrintableReadOnlyDictionary<string, string>();
}
