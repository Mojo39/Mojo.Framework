using OneOf;
using ValueOf;

namespace Mojo.Framework.Core.Exceptions;

public class ItemSelector : ValueOf<OneOf<object, string, IReadOnlyDictionary<string, object>>, ItemSelector>
{
    public object? Id => this.Value.Match(id => id, _ => null, _ => null);

    public string Key => this.Value.Match(_ => string.Empty, key => key, _ => string.Empty);

    public IReadOnlyDictionary<string, object> Query => this.Value.Match(
        _ => new Dictionary<string, object>(),  
        _ => new Dictionary<string, object>(), 
        query => query);
}
