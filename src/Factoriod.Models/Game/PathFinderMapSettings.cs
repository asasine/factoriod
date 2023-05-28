using System.Text.Json.Serialization;

namespace Factoriod.Models.Game;

public record PathFinderMapSettings(
    [property: JsonPropertyName("fwd2bwd_ratio")]
    uint Fwd2BwdRatio = 5,
    double GoalPressureRatio = 2,
    double MaxStepsWorkedPerTick = 1000,
    uint MaxWorkDonePerTick = 8000,
    bool UsePathCache = true,
    uint ShortCacheSize = 5,
    uint LongCacheSize = 25,
    double ShortCacheMinCacheableDistance = 10,
    uint ShortCacheMinAlgoStepsToCache = 50,
    double LongCacheMinCacheableDistance = 30,
    uint CacheMaxConnectToCacheStepsMultiplier = 100,
    double CacheAcceptPathStartDistanceRatio = 0.2,
    double CacheAcceptPathEndDistanceRatio = 0.15,
    double NegativeCacheAcceptPathStartDistanceRatio = 0.3,
    double NegativeCacheAcceptPathEndDistanceRatio = 0.3,
    double CachePathStartDistanceRatingMultiplier = 10,
    double CachePathEndDistanceRatingMultiplier = 20,
    double StaleEnemyWithSameDestinationCollisionPenalty = 30,
    double IgnoreMovingEnemyCollisionDistance = 5,
    double EnemyWithDifferentDestinationCollisionPenalty = 30,
    double GeneralEntityCollisionPenalty = 10,
    double GeneralEntitySubsequentCollisionPenalty = 3,
    double ExtendedCollisionPenalty = 3,
    uint MaxClientsToAcceptAnyNewRequest = 10,
    uint MaxClientsToAcceptShortNewRequest = 100,
    uint DirectDistanceToConsiderShortRequest = 100,
    uint ShortRequestMaxSteps = 1000,
    double ShortRequestRatio = 0.5,
    uint MinStepsToCheckPathFindTermination = 2000,
    double StartToGoalCostMultiplierToTerminatePathFind = 2000.0,
    PrintableReadOnlyCollection<uint>? OverloadLevels = null,
    PrintableReadOnlyCollection<uint>? OverloadMultipliers = null,
    uint NegativePathCacheDelayInterval = 20
)
{
    public PrintableReadOnlyCollection<uint> OverloadLevels { get; init; } = OverloadLevels ?? new(new uint[] { 0, 100, 500 });
    public PrintableReadOnlyCollection<uint> OverloadMultipliers { get; init; } = OverloadMultipliers ?? new(new uint[] { 2, 3, 4 });
}
