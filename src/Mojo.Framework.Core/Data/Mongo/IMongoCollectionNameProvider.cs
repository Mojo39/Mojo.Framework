namespace Mojo.Framework.Core.Data.Mongo;

public interface IMongoCollectionNameProvider
{
    string GetCollectionName<TDocument>();
}