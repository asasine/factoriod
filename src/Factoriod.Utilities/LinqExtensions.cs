namespace Factoriod.Utilities;

public static class LinqExtensions
{
    public static IEnumerable<T?> AsNullable<T>(this IEnumerable<T> source)
        where T : struct
        => source.Select(value => (T?)value);
}
