using System.ComponentModel.DataAnnotations;

namespace Factoriod.Models.Game;

public record AutoplaceControl(
    [Range(0, 6)]
    float Frequency,

    [Range(0, 6)]
    float Size,

    [Range(0, 6)]
    float Richness
);
