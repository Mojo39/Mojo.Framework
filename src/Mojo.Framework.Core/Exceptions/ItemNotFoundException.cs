namespace Mojo.Framework.Core.Exceptions;

public class ItemNotFoundException : Exception
{
    public ItemSelector Selector { get; private set; }

    public ItemNotFoundException(ItemSelector selector) => this.Selector = selector;

    public ItemNotFoundException(ItemSelector selector, Exception innerException)
        : base(innerException.Message, innerException) => this.Selector = selector;
}
