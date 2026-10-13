using Accessors.Activity;
using Accessors.Delays;
using Accessors.Notifications;
using Accessors.Projects;
using Accessors.Speech;
using Accessors.Users;
using Accessors.WorkItems;
using Microsoft.Extensions.DependencyInjection;
using Utilities;

namespace Accessors;

/// <summary>Registers and resolves accessor interfaces within the current request scope.</summary>
public class AccessorFactory(IServiceProvider services)
{
    public static void RegisterServices(IServiceCollection services)
    {
        UtilityFactory.RegisterServices(services);
        services.AddScoped<AccessorFactory>();
        services.AddScoped<IUserAccessor, UserAccessor>();
        services.AddScoped<IProjectAccessAccessor, ProjectAccessAccessor>();
        services.AddScoped<IDelayAccessor, DelayAccessor>();
        services.AddScoped<IDelayWorkflowAccessor, DelayWorkflowAccessor>();
        services.AddScoped<IActivityAccessor, ActivityAccessor>();
        services.AddScoped<ISpeechAccessor, SpeechAccessor>();
        services.AddScoped<INotificationAccessor, NotificationAccessor>();
        services.AddScoped<IWorkItemAccessor, WorkItemAccessor>();
    }

    public TInterface CreateAccessor<TInterface>() where TInterface : class =>
        services.GetRequiredService<TInterface>();
}
