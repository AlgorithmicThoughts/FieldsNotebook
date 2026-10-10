using Microsoft.Extensions.DependencyInjection;

namespace Accessors;

/// <summary>Resolves registered accessor interfaces in the current request scope.</summary>
public class AccessorFactory(IServiceProvider services)
{
    public TInterface CreateAccessor<TInterface>() where TInterface : class =>
        services.GetRequiredService<TInterface>();
}