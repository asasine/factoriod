using System.Text.RegularExpressions;

namespace Factoriod.Rcon;

internal static class FactorioCommandParser
{
    /// <summary>
    /// Parses the output of the <c>/players online</c> command and returns an enumerable of online players.
    /// </summary>
    /// <param name="input">The result of the <c>/players online</c> command.</param>
    /// <returns>An enumerable of online players.</returns>
    /// <exception cref="InvalidDataException">If <paramref name="input"/> is malformed.</exception>
    public static IEnumerable<string> OnlinePlayers(string input)
    {
        var match = Regex.Match(input, @"\G(^Online players \((?<num>\d+)\):)|((\s+|$)(?<player>\w+))", RegexOptions.ExplicitCapture);
        if (!match.Success)
        {
            throw new InvalidDataException("The input was in an unexpected format.");
        }

        var numPlayers = int.Parse(match.Groups["num"].ValueSpan);
        for (var i = 0; i < numPlayers; i++)
        {
            match = match.NextMatch();
            if (!match.Success)
            {
                throw new InvalidDataException($"Expected to find {numPlayers} but only found {i + 1}.");
            }

            yield return match.Groups["player"].Value;
        }

        match = match.NextMatch();
        if (match.Success)
        {
            throw new InvalidDataException($"Expected to only find {numPlayers} but there are more.");
        }
    }
}
