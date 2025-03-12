namespace Mojo.Framework.Core.Exceptions;

public class DuplicateFoundException : Exception
{
    public ItemSelector Selector { get; private set; }

    public DuplicateFoundException(ItemSelector selector) => this.Selector = selector;

    public DuplicateFoundException(ItemSelector selector, Exception innerException)
        : base(innerException.Message, innerException) => this.Selector = selector;
}
