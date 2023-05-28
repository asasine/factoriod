namespace Factoriod.Models.Game;

public record PollutionMapSettings(
    bool Enabled = true,
    double DiffusionRatio = 0.02,
    double MinToDiffuse = 15,
    double Ageing = 1,
    double ExpectedMaxPerChunk = 150,
    double MinToShowPerChunk = 50,
    double MinPollutionToDamageTrees = 60,
    double PollutionWithMaxForestDamage = 150,
    double PollutionPerTreeDamage = 50,
    double PollutionRestoredPerTreeDamage = 10,
    double MaxPollutionToRestoreTrees = 20,
    double EnemyAttackPollutionConsumptionModifier = 1
);
