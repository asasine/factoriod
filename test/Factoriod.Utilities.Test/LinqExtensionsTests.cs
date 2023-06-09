using System.Diagnostics.CodeAnalysis;

namespace Factoriod.Utilities.Test;

public class LinqExtensionsTests
{
    [Fact]
    public void AsNullableRetainsValues()
    {
        var values = Enumerable.Range(0, 10).ToList();
        var nullableValues = values.AsNullable().ToList();
        Assert.All(nullableValues, (value, index) => Assert.Equal(values[index], value));
    }

    [Fact]
    public void EmptyWithDefaultsReturnsEquivalent()
    {
        var empty = new Dictionary<int, int>();
        var defaults = Enumerable.Range(0, 10)
            .ToDictionary(x => x, x => 0);

        var actual = empty.WithDefaults(defaults);
        Assert.Equal(defaults, actual);
    }

    [Fact]
    public void WithDefaultsNonOverlapping()
    {
        var zeroToFour = Enumerable.Range(0, 5)
            .ToDictionary(x => x, x => 0);

        var fiveToNine = Enumerable.Range(5, 5)
            .ToDictionary(x => x, x => 1);

        var actual = zeroToFour.WithDefaults(fiveToNine);
        Assert.Equal(10, actual.Count);
        foreach (var keyValue in zeroToFour.Union(fiveToNine))
        {
            Assert.Contains(keyValue, actual);
        }
    }

    [Fact]
    public void WithDefaultsOverlapping()
    {
        var zeroToFour = Enumerable.Range(0, 5)
            .ToDictionary(x => x, x => 0);

        var zeroToNine = Enumerable.Range(0, 10)
            .ToDictionary(x => x, x => 1);

        var actual = zeroToFour.WithDefaults(zeroToNine);
        Assert.Equal(10, actual.Count);

        // everything in zeroToFour got added
        foreach (var keyValue in zeroToFour)
        {
            Assert.Contains(keyValue, actual);
        }

        // everything else in actual (excludes zeroToFour) is from zeroToNine
        var remaining = actual.ToHashSet()
            .Except(zeroToFour.ToHashSet())
            .ToDictionary(keyValue => keyValue.Key, keyvalue => keyvalue.Value);

        foreach (var keyValue in remaining)
        {
            Assert.Contains(keyValue, zeroToNine);
            Assert.DoesNotContain(keyValue, zeroToFour);
        }
    }
}
