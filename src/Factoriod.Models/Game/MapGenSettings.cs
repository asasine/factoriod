namespace Factoriod.Models.Game;

public record MapGenSettings(
    float TerrainSegmentation = 1,
    float Water = 1,
    PrintableReadOnlyDictionary<string, AutoplaceControl>? AutoplaceControls = null,
    PrintableReadOnlyDictionary<string, AutoplaceSettings>? AutoplaceSettings = null,
    CliffPlacementSettings? CliffSettings = null,
    uint? Seed = null,
    uint Width = 0,
    uint Height = 0,
    float StartingArea = 1,
    PrintableReadOnlyCollection<MapPosition>? StartingPoints = null,
    bool PeacefulMode = false,
    PrintableReadOnlyDictionary<string, string>? PropertyExpressionNames = null
)
{
    public PrintableReadOnlyDictionary<string, AutoplaceControl> AutoplaceControls { get; } = AutoplaceControls ?? new(new Dictionary<string, AutoplaceControl>
    {
        { "coal", new AutoplaceControl(1, 1, 1) },
        { "copper-ore", new AutoplaceControl(1, 1, 1) },
        { "crude-oil", new AutoplaceControl(1, 1, 1) },
        { "enemy-base", new AutoplaceControl(1, 1, 1) },
        { "iron-ore", new AutoplaceControl(1, 1, 1) },
        { "stone", new AutoplaceControl(1, 1, 1) },
        { "trees", new AutoplaceControl(1, 1, 1) },
        { "uranium-ore", new AutoplaceControl(1, 1, 1) },
    });

    public PrintableReadOnlyDictionary<string, AutoplaceSettings> AutoplaceSettings { get; } = AutoplaceSettings ?? new();
    public CliffPlacementSettings CliffSettings { get; } = CliffSettings ?? new();
    public PrintableReadOnlyCollection<MapPosition> StartingPoints { get; } = StartingPoints ?? new(new MapPosition[]
    {
        new MapPosition(0, 0),
    });

    public PrintableReadOnlyDictionary<string, string> PropertyExpressionNames { get; } = PropertyExpressionNames ?? new();
}
