using MongoDB.Driver;
using System.Runtime.CompilerServices;

namespace Mojo.Framework.Core.Data.Mongo.Extensions;

/// <summary>
///     A class of extension methods for <see cref="IAsyncCursor{T}"/>.
/// </summary>
public static class IAsyncCursorExtensions
{
    public static async IAsyncEnumerable<TEntity> ForEachAsync<TEntity>(
        IAsyncCursor<TEntity> cursor, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var item in cursor.Current)
            {
                yield return item;
            }
        }
    }

    public static async IAsyncEnumerable<TResult> ForEachAsync<TEntity, TResult>(
        IAsyncCursor<TEntity> cursor,
        Func<TEntity, TResult> processor,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var item in cursor.Current)
            {
                yield return processor(item);
            }
        }
    }
}
