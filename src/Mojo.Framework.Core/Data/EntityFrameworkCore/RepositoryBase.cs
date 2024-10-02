using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Mojo.Framework.Core.Data.Entities;
using Mojo.Framework.Core.Exceptions;
using Mojo.Framework.Core.Mapping;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Mojo.Framework.Core.Data.EntityFrameworkCore;

/// <summary>
/// An abstract class implements database access using the basic functionality of the Entity Framework Core.
/// </summary>
/// <typeparam name="TDomainModel">Type of domain entity.</typeparam>
/// <typeparam name="TDataKey">Type of database entity.</typeparam>
/// <typeparam name="TDataModel">Type of database entity.</typeparam>
public abstract class BaseRepository<TDomainModel, TDataKey, TDataModel>
    where TDomainModel : class
    where TDataKey : class
    where TDataModel : EntityBase<TDataKey>
{
    private readonly DbContext _dbContext;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{TDomainModel, TDataModel}" /> class.
    /// </summary>
    /// <param name="dbContext"><inheritdoc cref="DbContext" path="/summary"/></param>
    /// <param name="mapper"><inheritdoc cref="IMapper" path="/summary"/></param>
    protected BaseRepository(DbContext dbContext, IMapper mapper)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        Set = this._dbContext.Set<TDataModel>();
    }

    private DbSet<TDataModel> Set { get; }

    /// <summary>
    ///     An asynchronously method that adds a new element.
    /// </summary>
    /// <param name="entity">A new element.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary"/></param>
    /// <exception cref="DuplicateFoundException"><paramref name="entity"/> is already exists.</exception>
    /// <returns>
    ///     A task that represents the asynchronous operation. 
    ///     The task result contains an identifier of the new element.
    /// </returns>
    public virtual async Task<TDataKey> CreateAsync([NotNull] TDomainModel entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityDbo = Map(entity);

        try
        {
            if (entityDbo.Id == default && GeneratePrimaryKey(out var key))
            {
                entityDbo.Id = key;
            }

            _ = await Set.AddAsync(entityDbo, cancellationToken);

            return entityDbo.Id;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("is already being tracked", StringComparison.OrdinalIgnoreCase))
        {
            throw new DuplicateFoundException(entityDbo.Id, ex);
        }
        //catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlException && (sqlException.Number == 2627 || sqlException.Number == 2601))
        //{
        //    throw new DuplicateFoundException(entityDbo.Id, ex);
        //}
    }

    /// <summary>
    ///     An asynchronously method that deletes an element with identifier.
    /// </summary>
    /// <param name="id">The identifier of element to delete.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary" /></param>
    /// <exception cref="ItemNotFoundException">The element with <paramref name="id"/> not found.</exception>
    /// <exception cref="DuplicateFoundException">Items with more than one <paramref name="id"/>.</exception>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    /// </returns>
    public virtual async Task DeleteAsync(TDataKey id, CancellationToken cancellationToken)
    {
        var entity = await InternalGetByIdAsync(id, cancellationToken);
        _ = Set.Remove(entity);
    }

    /// <summary>
    ///     An asynchronously method that checks an element with identifier.
    /// </summary>
    /// <param name="id">The identifier of element to check.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary" /></param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains <see langword="true" /> if the table already exists, <see langword="false" /> otherwise.
    /// </returns>
    public virtual Task<bool> ExistsAsync(TDataKey id, CancellationToken cancellationToken)
        => Set.AnyAsync(itm => itm.Id.Equals(id), cancellationToken);

    /// <summary>
    ///     An asynchronously method that returns all elements.
    /// </summary>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary" /></param>
    /// <returns>
    ///     A task that represents the asynchronous operation. 
    ///     The task result contains an array with all elements ordered by identifier.
    /// </returns>
    public virtual IAsyncEnumerable<TDomainModel> GetAllAsync(CancellationToken cancellationToken)
        => ListAsync(
            filterBy: QueryHelper.Empty<TDataModel>(), 
            orderBy: QueryHelper.DefaultOrder<TDataModel, TDataKey>(), 
            cancellationToken: cancellationToken);

    /// <summary>
    ///     An asynchronously method that returns element by identifier.
    /// </summary>
    /// <param name="id">The identifier of element to return.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary" /></param>
    /// <exception cref="ItemNotFoundException">An element with <paramref name="id"/> not exists.</exception>
    /// <exception cref="DuplicateFoundException">Items with more than one <paramref name="id"/>.</exception>
    /// <returns>
    ///     A task that represents the asynchronous operation. 
    ///     The task result contains the single element, or <see langword="default" /> if no such element is found.
    /// </returns>
    public virtual async Task<TDomainModel> GetByIdAsync(TDataKey id, CancellationToken cancellationToken)
    {
        var entity = await InternalGetByIdAsync(id, cancellationToken);

        return Map(entity);
    }

    /// <summary>
    ///     An asynchronously method that modifies element.
    /// </summary>
    /// <param name="id">The identified of element to modify.</param>
    /// <param name="entity">New data to modify.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary" /></param>
    /// <exception cref="ItemNotFoundException">An element with <paramref name="id"/> not exists.</exception>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    /// </returns>
    public async Task<TDomainModel> UpdateAsync(TDataKey id, TDomainModel entity, CancellationToken cancellationToken)
    {
        var entityDbo = Map(entity);
        entityDbo.Id = id;

        var entry = _dbContext.Entry(entityDbo);
        if (entry.State is not EntityState.Detached)
        {
            entry.State = EntityState.Modified;
        }
        else
        {
            var attachedEntity = await InternalGetByIdAsync(entityDbo.Id, cancellationToken);
            if (attachedEntity is not null)
            {
                var attachedEntry = _dbContext.Entry(attachedEntity);
                ApplyState(_dbContext, attachedEntry, entry);
            }

            entityDbo = attachedEntity;
        }

        return Map(entityDbo);
    }

    private static void ApplyState(DbContext context, EntityEntry attachedEntry, EntityEntry entry)
    {
        attachedEntry.CurrentValues.SetValues(entry.Entity);
        foreach (var item in attachedEntry.Navigations)
        {
            var entryNav = entry.Navigation(item.Metadata.Name);
            if (entryNav.CurrentValue is not null)
            {
                var attachedItemEntry = context.Attach(item.CurrentValue);
                attachedItemEntry.CurrentValues.SetValues(entryNav.CurrentValue);
            }
        }
    }

    /// <summary>
    ///     A method that generates new identifier.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    ///     The task result contains <see langword="true" /> if key is generated, <see langword="false" /> otherwise.
    /// </returns>
    protected virtual bool GeneratePrimaryKey(out TDataKey key)
    {
        key = default;

        // todo: implement method.
        return true;
    }

    /// <summary>
    ///     An asynchronously method that return sorted array of elements.
    /// </summary>
    /// <typeparam name="TOrderKey">Type of value of sorting.</typeparam>
    /// <param name="filterBy">Expression of filtering the elements.</param>
    /// <param name="orderBy">Expression of sorting the elements.</param>
    /// <param name="cancellationToken"><inheritdoc cref="CancellationToken" path="/summary" /></param>
    /// <returns>
    ///     A task that represents the asynchronous operation.
    ///     The task result contains the ordered array of elements that satisfies the filtering condition.
    /// </returns>
    protected virtual async IAsyncEnumerable<TDomainModel> ListAsync<TOrderKey>(
        Expression<Func<TDataModel, bool>> filterBy,
        Expression<Func<TDataModel, TOrderKey>> orderBy,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var entities = await IncludeForMultiplyQuery(Set)
            .AsNoTracking()
            .Where(filterBy)
            .OrderBy(orderBy)
            .ToListAsync(cancellationToken);

        foreach (var item in entities)
        {
            yield return Map(item);
        }
    }

    /// <summary>
    ///     A method that loads related entities to the method <see cref="ListAsync" />.
    /// </summary>
    /// <param name="query">An <see cref="IQueryable{T}"/>.</param>
    /// <returns>
    ///     An <see cref="IQueryable{TDataModel}" /> that includes the related entities.
    /// </returns>
    protected virtual IQueryable<TDataModel> IncludeForMultiplyQuery(IQueryable<TDataModel> query) => query;

    /// <summary>
    ///     A method to maps of <typeparamref name="TDataModel" /> to <typeparamref name="TDomainModel" />.
    /// </summary>
    /// <param name="entity">Source object <typeparamref name="TDataModel"/> to map from.</param>
    /// <returns>
    ///     Mapped destination object <typeparamref name="TDomainModel"/>.
    /// </returns>
    protected virtual TDomainModel Map(TDataModel entity) => _mapper.Map<TDataModel, TDomainModel>(entity);

    /// <summary>
    ///     A method to maps of <typeparamref name="TDomainModel" /> to <typeparamref name="TDataModel" />.
    /// </summary>
    /// <param name="entity">Source object <typeparamref name="TDomainModel"/> to map from.</param>
    /// <returns>
    ///     Mapped destination object <typeparamref name="TDataModel"/>.
    /// </returns>
    protected virtual TDataModel Map(TDomainModel entity) => _mapper.Map<TDomainModel, TDataModel>(entity);

    /// <summary>
    ///     A method that loads related entities to the method <see cref="GetByIdAsync" />.
    /// </summary>
    /// <param name="query">An <see cref="IQueryable{T}"/>.</param>
    /// <returns>
    ///     An <see cref="IQueryable{TDataModel}" /> that includes the related entities.
    /// </returns>
    protected virtual IQueryable<TDataModel> IncludeForSingleQuery(IQueryable<TDataModel> query) => query;

    private async Task<TDataModel> InternalGetByIdAsync(TDataKey id, CancellationToken cancellationToken)
    {
        var entities = await IncludeForSingleQuery(Set).Where(itm => itm.Id.Equals(id)).ToListAsync(cancellationToken);

        return entities switch
        {
            { Count: 0 } => throw new ItemNotFoundException(id),
            { Count: 1 } => entities.Single(),
            { Count: > 1 } => throw new DuplicateFoundException(id),
            _ => throw new NotImplementedException()
        };
    }
}