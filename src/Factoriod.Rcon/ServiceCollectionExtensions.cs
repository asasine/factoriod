using Microsoft.Extensions.DependencyInjection;

namespace Factoriod.Rcon.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRconClient(this IServiceCollection services, string configSectionPath)
    {
        services.AddOptions<RconOptions>()
            .BindConfiguration(configSectionPath)
            .ValidateDataAnnotations();

        services.AddSingleton<RconClient>();

        return services;
    }
}