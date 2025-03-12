using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Mojo.Framework.Core.Data.Mongo.Options;
using MongoDB.Driver;

namespace Mojo.Framework.Core.Data.Mongo;

public interface IMongoDatabaseProvider
{
    IMongoDatabase GetDatabase();
}

/// <inheritdoc cref="IMongoDatabaseProvider"/>
public class MongoDatabaseProvider(
    IMongoClientProvider _mongoClientProvider, 
    IConfiguration _configuration, 
    IOptions<MongoDatabaseProviderOptions> _options) : IMongoDatabaseProvider
{
    private const string ConnectionStringName = "Mongo";

    public IMongoDatabase GetDatabase()
    {
        var mongoClient = _mongoClientProvider.GetMongoClient();
        var connectionString = _configuration.GetConnectionString(ConnectionStringName);
        var databaseName = MongoUrl.Create(connectionString).DatabaseName ??
            _options.Value.FallbackDatabaseName ?? throw new ArgumentNullException("Database name is not provided by connection string or fall-back configuration.");

        return mongoClient.GetDatabase(databaseName);
    }
}
