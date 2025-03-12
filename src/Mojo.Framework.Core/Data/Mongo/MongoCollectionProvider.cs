using MongoDB.Driver;

namespace Mojo.Framework.Core.Data.Mongo;

public interface IMongoCollectionProvider
{
    IMongoCollection<TEntity> GetCollection<TEntity>();
}

public class MongoCollectionProvider(
    IMongoDatabaseProvider mongoDatabaseProvider,
    IMongoCollectionNameProvider mongoCollectionNameProvider) : IMongoCollectionProvider
{
    public IMongoCollection<TDocument> GetCollection<TDocument>() => mongoDatabaseProvider.GetDatabase()
        .GetCollection<TDocument>(mongoCollectionNameProvider.GetCollectionName<TDocument>());
}
