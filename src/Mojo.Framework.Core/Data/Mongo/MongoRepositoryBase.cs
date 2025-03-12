using Microsoft.EntityFrameworkCore;
using Mojo.Framework.Core.Data.Entities;
using Mojo.Framework.Core.Exceptions;
using Mojo.Framework.Core.Mapping;
using MongoDB.Driver;

namespace Mojo.Framework.Core.Data.Mongo;

/// <summary>
/// An abstract class implements database access using the basic functionality of the MongoDB.
/// </summary>
/// <typeparam name="TDomainModel">Type of domain entity.</typeparam>
/// <typeparam name="TDataKey">Type of database entity.</typeparam>
/// <typeparam name="TDataModel">Type of database entity.</typeparam>
public abstract class MongoRepositoryBase<TDomainModel, TDataKey, TDataModel>
    where TDomainModel : class
    where TDataModel : EntityBase<TDataKey>
{
    private readonly IMongoSessionProvider _mongoClientSessionProvider;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoRepositoryBase{TDomainModel, TDataKey, TDataModel}" /> class.
    /// </summary>
    /// <param name="mongoSessionProvider"></param>
    /// <param name="mongoCollectionProvider"></param>
    /// <param name="mapper"><inheritdoc cref="IMapper" path="/summary"/></param>
    protected MongoRepositoryBase(IMongoSessionProvider mongoSessionProvider, IMongoCollectionProvider mongoCollectionProvider, IMapper mapper)
    {
        this._mongoClientSessionProvider = mongoSessionProvider ?? throw new ArgumentNullException(nameof(mongoSessionProvider));
        this._mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        this.Collection = mongoCollectionProvider.GetCollection<TDataModel>();
    }

    private IMongoCollection<TDataModel> Collection { get; }

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
    public virtual async Task<TDataKey> CreateAsync(TDomainModel entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityDbo = Map(entity);

        if (GeneratePrimaryKey(out var key))
        {
            entityDbo.Id = key;
        }

        try
        {
            await Collection.InsertOneAsync(this.GetClientSessionHandle(), entityDbo, cancellationToken: cancellationToken);
            return entityDbo.Id!;

        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateFoundException(ItemSelector.From(entityDbo.Id!), ex);
        }
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
    public virtual Task DeleteAsync(TDataKey id, CancellationToken cancellationToken)
     => Collection.DeleteManyAsync(
            this.GetClientSessionHandle(),
            QueryHelper.FilterById<TDataModel, TDataKey>(id),
            cancellationToken: cancellationToken);

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
        => Collection
        .Find(
            this.GetClientSessionHandle(),
            QueryHelper.FilterById<TDataModel, TDataKey>(id))
        .AnyAsync(cancellationToken);

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
        var entities = await Collection
            .FindSync(
                this.GetClientSessionHandle(),
                QueryHelper.FilterById<TDataModel, TDataKey>(id),
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);

        return entities switch
        {
            { Count: 0 } => throw new ItemNotFoundException(ItemSelector.From(id!)),
            { Count: 1 } => Map(entities.Single()),
            { Count: > 1 } => throw new DuplicateFoundException(ItemSelector.From(id!)),
            _ => throw new NotImplementedException()
        };
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

        var result = await Collection.ReplaceOneAsync(
            this.GetClientSessionHandle(),
            QueryHelper.FilterById<TDataModel, TDataKey>(id),
            entityDbo,
            cancellationToken: cancellationToken);

        return Map(entityDbo);
    }

    /// <summary>
    ///     A method that generates new identifier.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    ///     The task result contains <see langword="true" /> if key is generated, <see langword="false" /> otherwise.
    /// </returns>
    protected abstract bool GeneratePrimaryKey(out TDataKey key);

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
    protected virtual IAsyncEnumerable<TDomainModel> ListAsync(
        FilterDefinition<TDataModel> filterBy,
        SortDefinition<TDataModel> orderBy,
        CancellationToken cancellationToken)
    {
        var cursor = Collection.FindSync(
            this.GetClientSessionHandle(),
            filterBy,
            new FindOptions<TDataModel>() { Sort = orderBy },
            cancellationToken: cancellationToken);

        return Extensions.IAsyncCursorExtensions.ForEachAsync<TDataModel, TDomainModel>(cursor, itm => Map(itm), cancellationToken);
    }

    protected IClientSessionHandle GetClientSessionHandle() => this._mongoClientSessionProvider.GetClientSessionHandle();

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
}