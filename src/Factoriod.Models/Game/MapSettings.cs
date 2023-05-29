namespace Factoriod.Models.Game;

public record MapSettings(
    PollutionMapSettings? Pollution,
    EnemyEvolutionMapSettings? EnemyEvolution,
    EnemyExpansionMapSettings? EnemyExpansion,
    UnitGroupMapSettings? UnitGroup,
    SteeringMapSettings? Steering,
    PathFinderMapSettings? PathFinder,
    uint MaxFailedBehaviorCount = 3
)
{
    public PollutionMapSettings Pollution { get; } = Pollution ?? new();
    public EnemyEvolutionMapSettings EnemyEvolution { get; } = EnemyEvolution ?? new();
    public EnemyExpansionMapSettings EnemyExpansion { get; } = EnemyExpansion ?? new();
    public UnitGroupMapSettings UnitGroup { get; } = UnitGroup ?? new();
    public SteeringMapSettings Steering { get; } = Steering ?? new();
    public PathFinderMapSettings PathFinder { get; } = PathFinder ?? new();
}
