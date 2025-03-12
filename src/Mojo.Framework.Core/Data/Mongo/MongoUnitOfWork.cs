using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Mojo.Framework.Core.Data.Mongo
{
    /// <summary>
    /// Implementation of Unit of work for MongoDB.
    /// </summary>
    public class MongoUnitOfWork(IServiceScope serviceScope, IClientSessionHandle clientSessiontHandle) : IUnitOfWork
    {
        public IServiceProvider Services => serviceScope.ServiceProvider;

        public Task CompleteAsync(CancellationToken cancellationToken) => clientSessiontHandle.CommitTransactionAsync(cancellationToken);

        public Task DiscardAsync(CancellationToken cancellationToken) => clientSessiontHandle.AbortTransactionAsync(cancellationToken);

        public void Dispose()
        {
            serviceScope?.Dispose();

            if (IsDirty())
            {
                throw new InvalidOperationException("Unit of work disposed without completion.");
            }
        }

        public ValueTask DisposeAsync() => throw new NotImplementedException();

        protected bool IsDirty() => clientSessiontHandle.WrappedCoreSession.IsDirty;
    }
}
