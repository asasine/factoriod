using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Factoriod.Models.Game;

public record CliffPlacementSettings(
    string Name = "cliff",

    [property: JsonPropertyName("cliff_elevation_0")]
    float CliffElevation0 = 10,
    float CliffElevationInterval = 40,

    [Range(0, 6)]
    float Richness = 1
);
