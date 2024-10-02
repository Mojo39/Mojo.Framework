namespace Mojo.Framework.Core.Exceptions;

internal class DuplicateFoundException : Exception
{
    public object Key { get; private set; }

    public DuplicateFoundException(object key) => this.Key = key;

    public DuplicateFoundException(object key, Exception innerException)
        : base(innerException.Message, innerException) => this.Key = key;
}
