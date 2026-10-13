using Accessors;
using Engines;
using Managers.Delays;
using Managers.Notifications;
using Managers.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utilities;

namespace Managers;

/// <summary>Registers every layer below the client, and resolves manager interfaces within the current request scope.</summary>
public class ManagerFactory(IServiceProvider services)
{
    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        UtilityFactory.RegisterServices(services);
        AccessorFactory.RegisterServices(services, configuration);
        EngineFactory.RegisterServices(services);
        services.AddScoped<ManagerFactory>();
        services.AddScoped<IDelayManager, DelayManager>();
        services.AddScoped<IDelayInsightManager, DelayInsightManager>();
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<INotificationManager, NotificationManager>();
    }

    public TInterface CreateManager<TInterface>() where TInterface : class, IManager =>
        services.GetRequiredService<TInterface>();
}
