using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Mojo.Framework.Core.Data.Mongo;

public interface IMongoClientProvider
{
    IMongoClient GetMongoClient();
}

/// <inheritdoc cref="IMongoClientProvider"/>
public class MongoClientProvider(IConfiguration _configuration) : IMongoClientProvider
{
    private const string ConnectionStringName = "Mongo";

    private object _locker = new object();

    private IMongoClient _client;

    public IMongoClient GetMongoClient()
    {
        if (this._client is null)
        {
            lock (_locker)
            {
                var connectionString = _configuration.GetConnectionString(ConnectionStringName);
                var settings = MongoClientSettings.FromConnectionString(connectionString);

                this._client = new MongoClient(settings);
            }
        }

        return this._client;
    }
}
