namespace Factoriod.Models.Game;

public record MapExchangeStringData(
    MapAndDifficultySettings? MapSettings = null,
    MapGenSettings? MapGenSettings = null
)
{
    public MapAndDifficultySettings MapSettings { get; init; } = MapSettings ?? new();
    public MapGenSettings MapGenSettings { get; init; } = MapGenSettings ?? new();
}
