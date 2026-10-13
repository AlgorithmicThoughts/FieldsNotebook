using Engines.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Engines;

/// <summary>Registers and resolves engine interfaces within the current request scope.</summary>
public class EngineFactory(IServiceProvider services)
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<EngineFactory>();
        services.AddScoped<IAuthorizationEngine, AuthorizationEngine>();
    }

    public TInterface CreateEngine<TInterface>() where TInterface : class
    {
        return services.GetRequiredService<TInterface>();
    }
}
