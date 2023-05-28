using System.Collections;

namespace Factoriod.Models.Game;

public class PrintableReadOnlyCollection<TValue> : IReadOnlyCollection<TValue>
{
    private readonly IReadOnlyCollection<TValue> collection;
    public PrintableReadOnlyCollection(IReadOnlyCollection<TValue>? collection = null)
        => this.collection = collection ?? Array.Empty<TValue>();

    public int Count => collection.Count;

    public IEnumerator<TValue> GetEnumerator()
    {
        return collection.GetEnumerator();
    }

    public override string ToString()
        => $"{{ {string.Join(", ", this.Select(value => value?.ToString() ?? "<null>"))} }}";

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)collection).GetEnumerator();
    }
}
