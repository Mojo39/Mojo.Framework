namespace Mojo.Framework.Core.Data;

/// <summary>
///     Unit Of Work.
/// </summary>
/// <remarks>
///     See <see href="https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application">link</see>.
/// </remarks>
public interface IUnitOfWork : IAsyncDisposable
{
    Task CompleteAsync(CancellationToken cancellationToken);

    Task DiscardAsync(CancellationToken cancellationToken);
}
