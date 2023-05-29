using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Factoriod.Utilities;

public class PrintableReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly IReadOnlyDictionary<TKey, TValue> dictionary;
    public PrintableReadOnlyDictionary(IReadOnlyDictionary<TKey, TValue>? dictionary = null)
        => this.dictionary = dictionary ?? new Dictionary<TKey, TValue>();

    public TValue this[TKey key] => dictionary[key];

    public IEnumerable<TKey> Keys => dictionary.Keys;

    public IEnumerable<TValue> Values => dictionary.Values;

    public int Count => dictionary.Count;

    public bool ContainsKey(TKey key)
    {
        return dictionary.ContainsKey(key);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return dictionary.GetEnumerator();
    }

    public override string ToString()
        => $"{{ {string.Join(", ", this.Select(pair => $"\"{pair.Key}\": {pair.Value}"))} }}";

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return dictionary.TryGetValue(key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)dictionary).GetEnumerator();
    }
}
