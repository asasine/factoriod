namespace Factoriod.Utilities.Test;

public class PrintableReadOnlyCollectionTests
{
    [Fact]
    public void ConstructorUsesEmptyOnNull()
    {
        var actual = new PrintableReadOnlyCollection<int>((IReadOnlyCollection<int>?)null);
        Assert.Empty(actual);
    }

    [Fact]
    public void ConstructorReferencesParameter()
    {
        var collection = Enumerable.Range(0, 10).ToList();
        var actual = new PrintableReadOnlyCollection<int>(collection);
        Assert.Same(collection, actual.collection);
    }

    [Fact]
    public void ConstructorUsesInnermostCollection()
    {
        var inner = Enumerable.Range(0, 10).ToList();
        var collection = new PrintableReadOnlyCollection<int>(inner);
        for (int i = 0; i < 10; i++)
        {
            collection = new PrintableReadOnlyCollection<int>(collection);
        }

        var actual = new PrintableReadOnlyCollection<int>(collection);
        Assert.Same(inner, actual.collection);
    }

    [Fact]
    public void ToStringHasAllElements()
    {
        var collection = Enumerable.Range(0, 10).ToList();
        var actual = new PrintableReadOnlyCollection<int>(collection);
        var expected = $"{{ {string.Join(", ", collection)} }}"; // "{ 0, 1, 2, ..., 9 }"
        Assert.Equal(expected, actual.ToString());
    }
}
