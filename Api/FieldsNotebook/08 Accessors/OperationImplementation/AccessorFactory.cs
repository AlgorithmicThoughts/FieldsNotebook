using Accessors.Activity;
using Accessors.Delays;
using Accessors.Notifications;
using Accessors.Projects;
using Accessors.Speech;
using Accessors.Users;
using Accessors.WorkItems;
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accessors;

/// <summary>Registers the Resources and the accessor interfaces over them, and resolves accessors within the current request scope.</summary>
public class AccessorFactory(IServiceProvider services)
{
    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Supply the value through user-secrets or an environment variable; never commit it.
        var connectionString = configuration.GetConnectionString("FieldNotebook")
            ?? throw new InvalidOperationException("Connection string 'FieldNotebook' is not configured.");
        services.AddDbContext<FieldNotebookContext>(options => options.UseSqlServer(connectionString));
        services.AddOptions<SpeechOptions>().Bind(configuration.GetSection(SpeechOptions.SectionName));
        services.AddOptions<AzureBlobStorageOptions>().Bind(configuration.GetSection(AzureBlobStorageOptions.SectionName));

        services.AddScoped<AccessorFactory>();
        services.AddScoped<IUserAccessor, UserAccessor>();
        services.AddScoped<IProjectAccessor, ProjectAccessor>();
        services.AddScoped<IDelayAccessor, DelayAccessor>();
        services.AddScoped<IDelayWorkflowAccessor, DelayWorkflowAccessor>();
        services.AddScoped<IDelayPhotoAccessor, DelayPhotoAccessor>();
        services.AddScoped<IActivityAccessor, ActivityAccessor>();
        services.AddScoped<ISpeechAccessor, SpeechAccessor>();
        services.AddScoped<INotificationAccessor, NotificationAccessor>();
        services.AddScoped<IWorkItemAccessor, WorkItemAccessor>();
    }

    public TInterface CreateAccessor<TInterface>() where TInterface : class, IAccessor =>
        services.GetRequiredService<TInterface>();
}
