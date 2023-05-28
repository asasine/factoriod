namespace Factoriod.Models.Game;

public record EnemyEvolutionMapSettings(
    bool Enabled = true,
    double TimeFactor = 0.000004,
    double DestroyFactor = 0.002,
    double PollutionFactor = 0.0000009
);
