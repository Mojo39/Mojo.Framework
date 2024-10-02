namespace Mojo.Framework.Core.Exceptions;

public class ItemNotFoundException : Exception
{
    public object Key { get; private set; }

    public ItemNotFoundException(object key) => this.Key = key;

    public ItemNotFoundException(object key, Exception innerException)
        : base(innerException.Message, innerException) => this.Key = key;
}
