using Accessors;
using Accessors.Activity;
using Accessors.Delays;
using Accessors.Notifications;
using Accessors.Projects;
using Accessors.Speech;
using Accessors.Users;
using Accessors.WorkItems;
using Engines;
using Engines.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Utilities;
using Utilities.OperationContract;

namespace Tests.Managers;

/// <summary>Builds a Manager over mocked dependencies; any dependency a test does not supply is a loose stand-in.</summary>
internal sealed class ManagerTestHost
{
    private readonly ServiceCollection _services = new();

    public ManagerTestHost()
    {
        _services.AddSingleton(Mock.Of<IProjectAccessAccessor>());
        _services.AddSingleton(Mock.Of<IDelayAccessor>());
        _services.AddSingleton(Mock.Of<IDelayWorkflowAccessor>());
        _services.AddSingleton(Mock.Of<IActivityAccessor>());
        _services.AddSingleton(Mock.Of<INotificationAccessor>());
        _services.AddSingleton(Mock.Of<IUserAccessor>());
        _services.AddSingleton(Mock.Of<IWorkItemAccessor>());
        _services.AddSingleton(Mock.Of<ISpeechAccessor>());
        _services.AddSingleton(Mock.Of<IAzureBlobStorageUtility>());
        _services.AddSingleton<AccessorFactory>();
        _services.AddSingleton<UtilityFactory>();
        _services.AddSingleton<EngineFactory>();
        _services.AddSingleton<IAuthorizationEngine, AuthorizationEngine>();
    }

    public ManagerTestHost With<T>(Mock<T> mock) where T : class
    {
        _services.AddSingleton(mock.Object);
        return this;
    }

    public TManager Create<TManager>() where TManager : class
    {
        return ActivatorUtilities.CreateInstance<TManager>(_services.BuildServiceProvider());
    }
}
