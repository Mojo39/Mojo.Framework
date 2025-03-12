using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mojo.Framework.Core.Data.EntityFrameworkCore;

/// <summary>
/// Implementation of Unit of work for EntityFramework Code.
/// </summary>
internal class EntityUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly DbContext _dbContext;

    private bool _disposed;
    
    public EntityUnitOfWork(DbContext dbContext, IServiceProvider services)
    {
        this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        this.Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceProvider Services { get; }

    public virtual Task CompleteAsync(CancellationToken cancellationToken) => this._dbContext.SaveChangesAsync(cancellationToken);

    public async virtual Task DiscardAsync(CancellationToken cancellationToken) => this._dbContext.ChangeTracker.Clear();

    public ValueTask DisposeAsync() => DisposeAsync(true);
    
    protected async virtual ValueTask DisposeAsync(bool disposing)
    {
        if (this._disposed)
        {
            return;
        }

        if (disposing)
        {
            await _dbContext.DisposeAsync();
        }

        GC.SuppressFinalize(this);

        this._disposed = true;
    }
}