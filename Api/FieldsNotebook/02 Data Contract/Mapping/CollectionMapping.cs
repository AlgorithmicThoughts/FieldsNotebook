namespace DataContract.Mapping;

internal static class CollectionMapping
{
    /// <summary>Maps each item in order; a null collection maps to an empty list.</summary>
    public static IReadOnlyList<TOut> MapAll<TIn, TOut>(this IEnumerable<TIn>? source, Func<TIn, TOut> map)
    {
        if (source is null)
        {
            return [];
        }

        return source.Select(map).ToList();
    }
}
