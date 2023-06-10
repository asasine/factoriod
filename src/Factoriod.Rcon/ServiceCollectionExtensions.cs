using System.ComponentModel;
using System.Net;
using Factoriod.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace Factoriod.Rcon.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRconClient(this IServiceCollection services, string configSectionPath)
    {
        TypeDescriptor.AddAttributes(typeof(IPAddress), new TypeConverterAttribute(typeof(IPAddressTypeConverter)));
        services.AddOptions<RconOptions>()
            .BindConfiguration(configSectionPath)
            .ValidateDataAnnotations();

        services.AddSingleton<RconClient>();

        return services;
    }
}