using MongoDB.Driver;

namespace Mojo.Framework.Core.Data.Mongo;

public interface IMongoSessionProvider
{
    IClientSessionHandle GetClientSessionHandle();
}

public interface IMongoClientSessionStore
{
    void SetClientSessionHandle(IClientSessionHandle clientSessionHandle);
}

public class MongoClientSessionContext : IMongoSessionProvider, IMongoClientSessionStore
{
    private IClientSessionHandle? _clientSessionHandle;

    public IClientSessionHandle GetClientSessionHandle() => _clientSessionHandle ?? throw new ArgumentException("ClientSessionHandle is not provided.");

    public void SetClientSessionHandle(IClientSessionHandle clientSessionHandle)
    {
        if (clientSessionHandle == null)
        {
            throw new ArgumentNullException(nameof(clientSessionHandle));
        }

        _clientSessionHandle = clientSessionHandle;
    }
}
