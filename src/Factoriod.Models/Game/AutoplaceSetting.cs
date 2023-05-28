namespace Factoriod.Models.Game;

public record AutoplaceSettings(
    bool TreatMissingAsDefault,
    PrintableReadOnlyDictionary<string, AutoplaceControl> Settings
);
