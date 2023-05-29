using Factoriod.Utilities;

namespace Factoriod.Models.Game;

public record AutoplaceSettings(
    bool TreatMissingAsDefault,
    IReadOnlyDictionary<string, AutoplaceControl> Settings
)
{
    public IReadOnlyDictionary<string, AutoplaceControl> Settings { get; } = new PrintableReadOnlyDictionary<string, AutoplaceControl>(Settings);
}
