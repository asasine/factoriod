using System.ComponentModel.DataAnnotations;

namespace Factoriod.Models.Game;

public record DifficultySettings(
    RecipeDifficulty RecipeDifficulty = RecipeDifficulty.Normal,
    TechnologyDifficulty TechnologyDifficulty = TechnologyDifficulty.Normal,

    [Range(0.001, 1000)]
    double TechnologyPriceMultiplier = 1,

    [RegularExpression(@"^((after-victory)|(always)|(never))$", ErrorMessage = "Value must be one of after-victory, always, or never")]
    string ResearchQueueSetting = "after-victory"
);
