using Xunit;

namespace Factoriod.Rcon.Test;

public class FactorioCommandParserTests
{
    public static IEnumerable<object[]> OnlinePlayersTestCases()
    {
        yield return new object[] { "Online players (0):", Array.Empty<string>() };
        yield return new object[] {
            @"Online players (1):
  foo",
            new string[] { "foo" }
        };

        yield return new object[]
        {
            @"Online players (2):
  foo
  bar",
            new string[] { "foo", "bar" }
        };
    }

    [Theory]
    [MemberData(nameof(OnlinePlayersTestCases))]
    public void OnlinePlayers(string input, IEnumerable<string> expected)
    {
        var actual = FactorioCommandParser.OnlinePlayers(input).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InvalidInputThrows()
    {
        Assert.ThrowsAny<InvalidDataException>(() => FactorioCommandParser.OnlinePlayers("unexpected").ToList());
    }

    [Fact]
    public void LessPlayersThanExpectedThrows()
    {
        var input = @"Online players (2):
  foo";

        Assert.ThrowsAny<InvalidDataException>(() => FactorioCommandParser.OnlinePlayers(input).ToList());
    }

    [Fact]
    public void MorePlayersThanExpectedThrows()
    {
        var input = @"Online players (1):
  foo
  bar";

        Assert.ThrowsAny<InvalidDataException>(() => FactorioCommandParser.OnlinePlayers(input).ToList());
    }
}