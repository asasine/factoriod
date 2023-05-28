namespace Factoriod.Models.Game;

public record MapAndDifficultySettings(
    PollutionMapSettings? Pollution = null,
    EnemyEvolutionMapSettings? EnemyEvolution = null,
    EnemyExpansionMapSettings? EnemyExpansion = null,
    UnitGroupMapSettings? UnitGroup = null,
    SteeringMapSettings? Steering = null,
    PathFinderMapSettings? PathFinder = null,
    uint MaxFailedBehaviorCount = 3,
    DifficultySettings? DifficultySettings = null
)
    : MapSettings(Pollution, EnemyEvolution, EnemyExpansion, UnitGroup, Steering, PathFinder, MaxFailedBehaviorCount)
{
    public DifficultySettings DifficultySettings { get; } = DifficultySettings ?? new();
}
