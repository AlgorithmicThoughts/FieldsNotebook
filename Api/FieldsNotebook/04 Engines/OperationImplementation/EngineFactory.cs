using Engines.Crews;
using Engines.Delays;
using Microsoft.Extensions.DependencyInjection;

namespace Engines;

/// <summary>Registers and resolves engine interfaces within the current request scope.</summary>
public class EngineFactory(IServiceProvider services)
{
    public static void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<EngineFactory>();
        services.AddScoped<IDelayWorkflowEngine, DelayWorkflowEngine>();
        services.AddScoped<IDelayInsightEngine, DelayInsightEngine>();
        services.AddScoped<IDelaySpeechEngine, DelaySpeechEngine>();
        services.AddScoped<ICrewCoverageEngine, CrewCoverageEngine>();
    }

    public TInterface CreateEngine<TInterface>() where TInterface : class, IEngine
    {
        return services.GetRequiredService<TInterface>();
    }
}
