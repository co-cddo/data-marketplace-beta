using Cddo.Data.Marketplace.Api.Configuration;
using Cddo.Data.Marketplace.Logic.Configuration;
using Cddo.Data.Marketplace.Logic.DependencyInjection;

namespace Cddo.Data.Marketplace.Api.DependencyInjection;

public static class ServiceRegistrationExtension
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddTransient<IConfigurationKeys, ApiConfigurationKeys>();

        // Items defined in the Logic assembly should be defined within this call that runs within the logic
        // namespace to avoid unnecessary spilling of visibility outside the logic assembly
        services.AddLogicDependencies();

        return services;
    }
}
