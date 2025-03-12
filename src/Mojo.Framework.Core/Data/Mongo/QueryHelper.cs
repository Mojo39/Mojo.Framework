using Mojo.Framework.Core.Data.Entities;
using MongoDB.Driver;

namespace Mojo.Framework.Core.Data.Mongo;

public static class QueryHelper
{
    public static FilterDefinition<TEntity> FilterById<TEntity, TKey>(TKey id) 
        where TEntity : EntityBase<TKey>
        => Builders<TEntity>.Filter.Eq(itm => itm.Id, id);

    public static FilterDefinition<TEntity> Empty<TEntity>()
        => Builders<TEntity>.Filter.Empty;

    public static SortDefinition<TEntity> DefaultOrder<TEntity, TKey>()
        where TEntity : EntityBase<TKey>
        => Builders<TEntity>.Sort.Ascending(itm => itm.Id);
}
