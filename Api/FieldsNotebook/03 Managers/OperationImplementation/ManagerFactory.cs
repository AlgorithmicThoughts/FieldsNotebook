using Accessors;
using Managers.Delays;
using Managers.Home;
using Managers.Notifications;
using Managers.Reports;
using Managers.Upcoming;
using Managers.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Managers;

/// <summary>Registers and resolves manager interfaces within the current request scope.</summary>
public class ManagerFactory(IServiceProvider services)
{
    public static void RegisterServices(IServiceCollection services)
    {
        AccessorFactory.RegisterServices(services);
        services.AddScoped<ManagerFactory>();
        services.AddScoped<IDelayManager, DelayManager>();
        services.AddScoped<IDelayReportManager, DelayReportManager>();
        services.AddScoped<IDelaySpeechManager, DelaySpeechManager>();
        services.AddScoped<IUserManager, UserManager>();
        services.AddScoped<IHomeManager, HomeManager>();
        services.AddScoped<INotificationManager, NotificationManager>();
        services.AddScoped<IUpcomingManager, UpcomingManager>();
        services.AddScoped<IReportManager, ReportManager>();
    }

    public TInterface CreateManager<TInterface>() where TInterface : class =>
        services.GetRequiredService<TInterface>();
}
