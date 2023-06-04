using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Factoriod.Utilities;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TService"/> as a hosted service and a singleton.
    /// </summary>
    /// <typeparam name="TService">The service type to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to register <typeparamref name="TService"/> with.</param>
    /// <returns>The <paramref name="services"/> parameter.</returns>
    /// <remarks>
    /// This function allows for registering <typeparamref name="TService"/> as both a <see cref="IHostedService"/> and a singleton.
    /// Dependencies may then inject <typeparamref name="TService"/> into their types and receive the same instance that is managed by the hosted service runner.
    /// <typeparamref name="TService"/> is not registered as a <see cref="IHostedService"/>, as this negatively affects the hosted service runner.
    /// To inject the singleton instance into another dependency, use <typeparamref name="TService"/> directly.
    /// </remarks>
    public static IServiceCollection AddSingletonHostedService<TService>(this IServiceCollection services)
        where TService : class, IHostedService
        => services.AddSingleton<TService>()
            .AddHostedService<HostedServiceWrapper<TService>>();

    private class HostedServiceWrapper<TService> : IHostedService
        where TService : notnull, IHostedService
    {
        private readonly IHostedService service;

        public HostedServiceWrapper(TService service) => this.service = service;

        public Task StartAsync(CancellationToken cancellationToken) => service.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => service.StopAsync(cancellationToken);
    }
}

