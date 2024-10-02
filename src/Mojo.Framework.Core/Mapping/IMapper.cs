namespace Mojo.Framework.Core.Mapping;

public interface IMapper
{
    TDest Map<TSource, TDest>(TSource origin);
}
