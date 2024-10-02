namespace Mojo.Framework.Core.Data.Entities;

public abstract class EntityBase<TId>
{
    public TId? Id { get; set; }

    public Metadata Metadata { get; set; }
}
