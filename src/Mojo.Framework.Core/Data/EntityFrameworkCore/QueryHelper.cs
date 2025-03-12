using Mojo.Framework.Core.Data.Entities;
using System.Linq.Expressions;

namespace Mojo.Framework.Core.Data.EntityFrameworkCore;

internal static class QueryHelper
{
    internal static Expression<Func<T, bool>> Empty<T>() => item => true;

    internal static Expression<Func<TItem, TKey>> DefaultOrder<TItem, TKey>()
        where TItem : EntityBase<TKey> => item => item.Id;
}
