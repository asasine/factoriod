namespace Factoriod.Models.Game;

public record SteeringMapSettings(
    SteeringMapSetting? Default = null,
    SteeringMapSetting? Moving = null
)
{
    public SteeringMapSetting Default { get; } = Default ?? new(1.2, 1.2, 0.005, false);
    public SteeringMapSetting Moving { get; } = Moving ?? new(3, 3, 0.01, false);
}

public record SteeringMapSetting(
    double Radius,
    double SeparationFactor,
    double SeparationForce,
    bool ForceUnitFuzzyGotoBehavior
);
