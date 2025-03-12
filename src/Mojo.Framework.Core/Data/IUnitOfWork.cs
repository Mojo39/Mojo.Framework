namespace Mojo.Framework.Core.Data;

/// <summary>
///     Unit Of Work.
/// </summary>
/// <remarks>
///     See <see href="https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application">link</see>.
/// </remarks>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <inheritdoc cref="IServiceProvider" path="/summary"/>
    IServiceProvider Services { get; }

    /// <summary>
    ///     An asynchronously method that complete all changes.
    /// </summary>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    /// </returns>
    Task CompleteAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     An asynchronously method that discard all changes.
    /// </summary>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    /// </returns>
    Task DiscardAsync(CancellationToken cancellationToken);
}
