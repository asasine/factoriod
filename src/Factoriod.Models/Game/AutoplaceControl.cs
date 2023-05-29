using System.ComponentModel.DataAnnotations;

namespace Factoriod.Models.Game;

public record AutoplaceControl(
    [Range(0, 6)]
    float Frequency = 1,

    [Range(0, 6)]
    float Size = 1,

    [Range(0, 6)]
    float Richness = 1
);
