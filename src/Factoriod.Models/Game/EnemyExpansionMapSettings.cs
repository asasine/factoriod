namespace Factoriod.Models.Game;

public record EnemyExpansionMapSettings(
    bool Enabled = true,
    uint MaxExpansionDistance = 7,
    uint FriendlyBaseInfluenceRadius = 2,
    uint EnemyBuildingInfluenceRadius = 2,
    double BuildingCoefficient = 0.1,
    double OtherBaseCoefficient = 2.0,
    double NeighbouringChunkCoefficient = 0.5,
    double NeighbouringBaseChunkCoefficient = 0.4,
    double MaxCollidingTilesCoefficient = 0.9,
    uint SettlerGroupMinSize = 5,
    uint SettlerGroupMaxSize = 20,
    uint MinExpansionCooldown = 14000,
    uint MaxExpansionCooldown = 216000
);
