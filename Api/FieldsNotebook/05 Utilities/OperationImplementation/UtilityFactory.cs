using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Utilities.OperationContract;
using Utilities.OperationImplementation;

namespace Utilities;

/// <summary>Registers and resolves utility interfaces within the current request scope.</summary>
public class UtilityFactory(IServiceProvider services)
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<UtilityFactory>();
        services.AddScoped<IClockUtility, ClockUtility>();
    }

    public TInterface CreateUtility<TInterface>() where TInterface : class, IUtility =>
        services.GetRequiredService<TInterface>();
}
