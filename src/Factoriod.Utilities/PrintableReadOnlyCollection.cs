using System.Collections;

namespace Factoriod.Utilities;

public class PrintableReadOnlyCollection<TValue> : IReadOnlyCollection<TValue>
{
    internal readonly IReadOnlyCollection<TValue> collection;

    public PrintableReadOnlyCollection(IReadOnlyCollection<TValue>? collection = null)
    {
        collection ??= Array.Empty<TValue>();

        // flatten any inner instances of ourselves
        while (collection is PrintableReadOnlyCollection<TValue> inner)
        {
            collection = inner.collection;
        }

        this.collection = collection;
    }

    public PrintableReadOnlyCollection(params TValue[] collection)
        : this((IReadOnlyCollection<TValue>)collection)
    {
    }

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
