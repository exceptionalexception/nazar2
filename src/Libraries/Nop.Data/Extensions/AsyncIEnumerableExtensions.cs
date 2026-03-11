namespace System.Linq;

/// <summary>
/// Represents a grouping of elements with an async-enumerable sequence, compatible with .NET 10's built-in async LINQ.
/// </summary>
public interface IAsyncGrouping<out TKey, out TElement> : IAsyncEnumerable<TElement>
{
    TKey Key { get; }
}

/// <summary>
/// Internal adapter that wraps a synchronous IGrouping as an IAsyncGrouping.
/// </summary>
file sealed class AsyncGroupingAdapter<TKey, TElement>(IGrouping<TKey, TElement> inner)
    : IAsyncGrouping<TKey, TElement>
{
    public TKey Key => inner.Key;

    public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return inner.ToAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
    }
}

public static class AsyncIEnumerableExtensions
{
    /// <summary>
    /// Projects each element of an async-enumerable sequence into a new form by applying
    /// an asynchronous selector function to each member of the source sequence and awaiting
    /// the result.
    /// </summary>
    public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(this IEnumerable<TSource> source,
        Func<TSource, ValueTask<TResult>> predicate)
    {
        return source.ToAsyncEnumerable().Select(async (TSource item, CancellationToken _) => await predicate(item));
    }

    /// <summary>
    /// Projects each element of an already-async sequence into a new form by applying
    /// an asynchronous selector function and awaiting the result.
    /// </summary>
    public static IAsyncEnumerable<TResult> SelectAwait<TSource, TResult>(this IAsyncEnumerable<TSource> source,
        Func<TSource, ValueTask<TResult>> predicate)
    {
        return source.Select(async (TSource item, CancellationToken _) => await predicate(item));
    }

    /// <summary>
    /// Returns the first element of an async-enumerable sequence that satisfies the
    /// condition in the predicate, or a default value if no element satisfies the condition
    /// in the predicate.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task<TSource> FirstOrDefaultAwaitAsync<TSource>(this IEnumerable<TSource> source,
        Func<TSource, ValueTask<bool>> predicate)
    {
        return source.ToAsyncEnumerable().FirstOrDefaultAsync(async (item, _) => await predicate(item)).AsTask();
    }

    /// <summary>
    /// Determines whether all elements in an async-enumerable sequence satisfy a condition.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task<bool> AllAwaitAsync<TSource>(this IEnumerable<TSource> source,
        Func<TSource, ValueTask<bool>> predicate)
    {
        return source.ToAsyncEnumerable().AllAsync(async (item, _) => await predicate(item)).AsTask();
    }

    /// <summary>
    /// Projects each element of an async-enumerable sequence into an async-enumerable
    /// sequence and merges the resulting async-enumerable sequences into one async-enumerable sequence.
    /// </summary>
    public static IAsyncEnumerable<TResult> SelectManyAwait<TSource, TResult>(this IEnumerable<TSource> source,
        Func<TSource, Task<IList<TResult>>> predicate)
    {
        return SelectManyIterator(source, predicate);

        static async IAsyncEnumerable<TResult> SelectManyIterator(IEnumerable<TSource> src, Func<TSource, Task<IList<TResult>>> pred)
        {
            foreach (var item in src)
                foreach (var result in await pred(item))
                    yield return result;
        }
    }

    /// <summary>
    /// Projects each element of an async-enumerable sequence into an async-enumerable
    /// sequence and merges the resulting async-enumerable sequences into one async-enumerable sequence.
    /// </summary>
    public static IAsyncEnumerable<TResult> SelectManyAwait<TSource, TResult>(this IEnumerable<TSource> source,
        Func<TSource, Task<IEnumerable<TResult>>> predicate)
    {
        return SelectManyIterator(source, predicate);

        static async IAsyncEnumerable<TResult> SelectManyIterator(IEnumerable<TSource> src, Func<TSource, Task<IEnumerable<TResult>>> pred)
        {
            foreach (var item in src)
                foreach (var result in await pred(item))
                    yield return result;
        }
    }

    /// <summary>
    /// Projects each element of an already-async sequence into an async-enumerable sequence
    /// and merges the results into one async-enumerable sequence.
    /// </summary>
    public static IAsyncEnumerable<TResult> SelectManyAwait<TSource, TResult>(this IAsyncEnumerable<TSource> source,
        Func<TSource, Task<IAsyncEnumerable<TResult>>> predicate)
    {
        return SelectManyIterator(source, predicate);

        static async IAsyncEnumerable<TResult> SelectManyIterator(IAsyncEnumerable<TSource> src, Func<TSource, Task<IAsyncEnumerable<TResult>>> pred)
        {
            await foreach (var item in src)
                await foreach (var result in await pred(item))
                    yield return result;
        }
    }

    /// <summary>
    /// Filters the elements of an async-enumerable sequence based on an asynchronous predicate.
    /// </summary>
    public static IAsyncEnumerable<TSource> WhereAwait<TSource>(this IEnumerable<TSource> source,
        Func<TSource, ValueTask<bool>> predicate)
    {
        return source.ToAsyncEnumerable().Where(async (item, _) => await predicate(item));
    }

    /// <summary>
    /// Filters an already-async sequence based on an asynchronous predicate.
    /// </summary>
    public static IAsyncEnumerable<TSource> WhereAwait<TSource>(this IAsyncEnumerable<TSource> source,
        Func<TSource, ValueTask<bool>> predicate)
    {
        return source.Where(async (TSource item, CancellationToken _) => await predicate(item));
    }

    /// <summary>
    /// Determines whether any element in an async-enumerable sequence satisfies a condition.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task<bool> AnyAwaitAsync<TSource>(this IEnumerable<TSource> source,
        Func<TSource, ValueTask<bool>> predicate)
    {
        return source.ToAsyncEnumerable().AnyAsync(async (item, _) => await predicate(item)).AsTask();
    }

    /// <summary>
    /// Returns the only element of an async-enumerable sequence that satisfies the condition
    /// in the asynchronous predicate, or a default value if no such element exists.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task<TSource> SingleOrDefaultAwaitAsync<TSource>(this IEnumerable<TSource> source,
        Func<TSource, ValueTask<bool>> predicate)
    {
        return source.ToAsyncEnumerable().SingleOrDefaultAsync(async (item, _) => await predicate(item)).AsTask();
    }

    /// <summary>
    /// Creates a list from an async-enumerable sequence.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public static Task<List<TSource>> ToListAsync<TSource>(this IEnumerable<TSource> source)
    {
        return source.ToAsyncEnumerable().ToListAsync().AsTask();
    }

    /// <summary>
    /// Sorts the elements of a sequence in descending order according to a key obtained
    /// by invoking a transform function on each element and awaiting the result.
    /// </summary>
    public static IOrderedAsyncEnumerable<TSource> OrderByDescendingAwait<TSource, TKey>(
        this IEnumerable<TSource> source, Func<TSource, ValueTask<TKey>> keySelector)
    {
        return source.ToAsyncEnumerable().OrderByDescending(item => keySelector(item).GetAwaiter().GetResult());
    }

    /// <summary>
    /// Groups the elements of an async-enumerable sequence and selects the resulting
    /// elements by using a specified function.
    /// </summary>
    public static IAsyncEnumerable<IAsyncGrouping<TKey, TElement>> GroupByAwait<TSource, TKey, TElement>(
        this IEnumerable<TSource> source, Func<TSource, ValueTask<TKey>> keySelector,
        Func<TSource, ValueTask<TElement>> elementSelector)
    {
        return source.ToAsyncEnumerable()
            .GroupBy(
                item => keySelector(item).GetAwaiter().GetResult(),
                item => elementSelector(item).GetAwaiter().GetResult())
            .Select(g => (IAsyncGrouping<TKey, TElement>)new AsyncGroupingAdapter<TKey, TElement>(g));
    }

    /// <summary>
    /// Applies an accumulator function over an async-enumerable sequence, returning
    /// the result of the aggregation as a single element in the result sequence.
    /// </summary>
    public static ValueTask<TAccumulate> AggregateAwaitAsync<TSource, TAccumulate>(
        this IEnumerable<TSource> source, TAccumulate seed,
        Func<TAccumulate, TSource, ValueTask<TAccumulate>> accumulator)
    {
        return source.ToAsyncEnumerable().AggregateAsync(seed, async (acc, item, _) => await accumulator(acc, item));
    }

    /// <summary>
    /// Creates a dictionary from an async-enumerable sequence using the specified asynchronous
    /// key and element selector functions.
    /// </summary>
    public static ValueTask<Dictionary<TKey, TElement>> ToDictionaryAwaitAsync<TSource, TKey, TElement>(
        this IEnumerable<TSource> source, Func<TSource, ValueTask<TKey>> keySelector,
        Func<TSource, ValueTask<TElement>> elementSelector) where TKey : notnull
    {
        return source.ToAsyncEnumerable().ToDictionaryAsync(
            async (item, _) => await keySelector(item),
            async (item, _) => await elementSelector(item));
    }

    /// <summary>
    /// Creates a dictionary from an async-enumerable sequence using the specified asynchronous key selector;
    /// the source element itself is used as the dictionary value.
    /// </summary>
    public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAwaitAsync<TSource, TKey>(
        this IEnumerable<TSource> source, Func<TSource, ValueTask<TKey>> keySelector) where TKey : notnull
    {
        return source.ToAsyncEnumerable().ToDictionaryAsync(async (TSource item, CancellationToken _) => await keySelector(item));
    }

    /// <summary>
    /// Creates a dictionary from an already-async sequence using the specified asynchronous key selector;
    /// the source element itself is used as the dictionary value.
    /// </summary>
    public static ValueTask<Dictionary<TKey, TSource>> ToDictionaryAwaitAsync<TSource, TKey>(
        this IAsyncEnumerable<TSource> source, Func<TSource, ValueTask<TKey>> keySelector) where TKey : notnull
    {
        return source.ToDictionaryAsync(async (TSource item, CancellationToken _) => await keySelector(item));
    }

    /// <summary>
    /// Groups the elements of an async-enumerable sequence according to a specified key selector function.
    /// </summary>
    public static IAsyncEnumerable<IAsyncGrouping<TKey, TSource>> GroupByAwait<TSource, TKey>(
        this IEnumerable<TSource> source, Func<TSource, ValueTask<TKey>> keySelector)
    {
        return source.ToAsyncEnumerable()
            .GroupBy(item => keySelector(item).GetAwaiter().GetResult())
            .Select(g => (IAsyncGrouping<TKey, TSource>)new AsyncGroupingAdapter<TKey, TSource>(g));
    }

    /// <summary>
    /// Computes the sum of a sequence of System.Decimal values that are obtained by
    /// invoking a transform function on each element of the source sequence and awaiting the result.
    /// </summary>
    public static ValueTask<decimal> SumAwaitAsync<TSource>(this IEnumerable<TSource> source,
        Func<TSource, ValueTask<decimal>> selector)
    {
        return source.ToAsyncEnumerable().Select(async (TSource item, CancellationToken _) => await selector(item)).SumAsync();
    }
}