namespace Factoriod.Utilities;

public static class LinqExtensions
{
    public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> source)
        where T : struct
        => source.Cast<T?>();

    /// <summary>
    /// Merge two dictionaries, with the left dictionary taking precedence.
    /// </summary>
    /// <param name="left">The dictionary taking precedence.</param>
    /// <param name="right">The dictionary to add values from when <paramref name="left"/> does not have a value.</param>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <returns>A dictionary with the values from <paramref name="left"/> and <paramref name="right"/>.</returns>
    public static IReadOnlyDictionary<TKey, TValue> WithDefaults<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> left, IReadOnlyDictionary<TKey, TValue> right)
        where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>(right);
        foreach (var (key, value) in left)
        {
            result[key] = value;
        }

        return result;
    }
}
