using Microsoft.Extensions.DependencyInjection;

namespace Mojo.Framework.Core.Data.Mongo
{
    public class MongoUnitOfWorkFactory(
        IServiceProvider services,
        IMongoClientProvider clientProvider) : IUnitOfWorkFactory
    {
        public async Task<IUnitOfWork> CreateAsync(CancellationToken cancellationToken)
        {
            var client = clientProvider.GetMongoClient();
            var clientSessiontHandle = await client.StartSessionAsync(cancellationToken: cancellationToken);

            var serviceScope = services.CreateScope();

            var sessionStore = serviceScope.ServiceProvider.GetRequiredService<IMongoClientSessionStore>();
            sessionStore.SetClientSessionHandle(clientSessiontHandle);

            clientSessiontHandle.StartTransaction();

            return new MongoUnitOfWork(serviceScope, clientSessiontHandle);
        }
    }
}
