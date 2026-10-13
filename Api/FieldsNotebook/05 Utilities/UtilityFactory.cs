using Microsoft.Extensions.DependencyInjection;
using Utilities.OperationContract;
using Utilities.OperationImplementation;

namespace Utilities
{
    public class UtilityFactory(IServiceProvider services)
    {
        public static void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<UtilityFactory>();
            services.AddOptions<AzureBlobStorageOptions>().BindConfiguration("AzureBlobStorage");
            services.AddScoped<IAzureBlobStorageUtility, AzureBlobStorageUtility>();
        }

        public TInterface CreateUtility<TInterface>() where TInterface : class =>
            services.GetRequiredService<TInterface>();
    }
}
