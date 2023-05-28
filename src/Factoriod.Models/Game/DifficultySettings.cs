namespace Factoriod.Models.Game;

public record DifficultySettings(
    RecipeDifficulty RecipeDifficulty = RecipeDifficulty.Normal,
    TechnologyDifficulty TechnologyDifficulty = TechnologyDifficulty.Normal,
    double TechnologyPriceMultiplier = 1,
    string ResearchQueueSetting = "after-victory"
);
