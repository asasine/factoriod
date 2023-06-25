using System.Text.Json.Serialization;

namespace Factoriod.Models.Mods;

public record FactorioAuthentication(string Username, string Token)
{
    [JsonIgnore]
    public string QueryParameters => $"username={Username}&token={Token}";
}
