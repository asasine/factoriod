namespace Factoriod.Models.Mods;

public record FactorioAuthentication(string Username, string Token)
{
    public string QueryParameters => $"username={Username}&token={Token}";
}
