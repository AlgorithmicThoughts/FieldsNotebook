using Microsoft.Extensions.DependencyInjection;

namespace Managers;

/// <summary>Resolves registered manager interfaces in the current request scope.</summary>
public class ManagerFactory(IServiceProvider services)
{
    public TInterface CreateManager<TInterface>() where TInterface : class =>
        services.GetRequiredService<TInterface>();
}