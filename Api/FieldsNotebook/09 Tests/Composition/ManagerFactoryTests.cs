using Engines;
using Engines.Crews;
using Engines.Delays;
using Managers;
using Managers.Delays;
using Managers.Notifications;
using Managers.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Composition;

/// <summary>The client registers only through the Manager factory, so every layer below must resolve from it alone.</summary>
public class ManagerFactoryTests
{
    [Fact]
    public void RegisterServices_EveryLayer_ResolvesEachManagerAndEngineInARequestScope()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:FieldNotebook"] = "Server=unused;Database=unused" })
            .Build();
        var services = new ServiceCollection().AddLogging();
        ManagerFactory.RegisterServices(services, configuration);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var managers = scope.ServiceProvider.GetRequiredService<ManagerFactory>();
        var engines = scope.ServiceProvider.GetRequiredService<EngineFactory>();

        // Act
        IManager[] resolvedManagers =
        [
            managers.CreateManager<IDelayManager>(),
            managers.CreateManager<IDelayInsightManager>(),
            managers.CreateManager<INotificationManager>(),
            managers.CreateManager<IUserManager>(),
        ];
        IEngine[] resolvedEngines =
        [
            engines.CreateEngine<IDelayWorkflowEngine>(),
            engines.CreateEngine<IDelayInsightEngine>(),
            engines.CreateEngine<IDelaySpeechEngine>(),
            engines.CreateEngine<ICrewCoverageEngine>(),
        ];

        // Assert
        Assert.All(resolvedManagers, Assert.NotNull);
        Assert.All(resolvedEngines, Assert.NotNull);
    }

    [Fact]
    public void RegisterServices_NoConnectionString_FailsAtStartup()
    {
        // Act
        var register = () => ManagerFactory.RegisterServices(new ServiceCollection(), new ConfigurationBuilder().Build());

        // Assert
        var failure = Assert.Throws<InvalidOperationException>(register);
        Assert.Contains("FieldNotebook", failure.Message);
    }
}
