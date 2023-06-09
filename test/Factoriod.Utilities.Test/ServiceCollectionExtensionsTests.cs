using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Factoriod.Utilities.Test;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSingletonHostedServiceAddsSingletonAndHostedService()
    {
        // setup
        var serviceCollection = new ServiceCollection();

        // sanity check
        Assert.Empty(serviceCollection);

        // act
        serviceCollection.AddSingletonHostedService<NoOpService>();

        // assert
        Assert.Contains(serviceCollection, serviceDescriptor => serviceDescriptor.Lifetime == ServiceLifetime.Singleton
            && serviceDescriptor.ServiceType == typeof(NoOpService)
            && serviceDescriptor.ImplementationType == typeof(NoOpService));

        Assert.Contains(serviceCollection, serviceDescriptor => serviceDescriptor.Lifetime == ServiceLifetime.Singleton
            && serviceDescriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void InstanceIsSingleton()
    {
        // asserts that only a single instance of the IHostedService is created, even if the service is acquired as both its type and as IHostedService
        // setup
        var serviceCollection = new ServiceCollection();
        NoOpService.instanceCounter = 0;

        // act
        serviceCollection.AddSingletonHostedService<NoOpService>();
        var services = serviceCollection.BuildServiceProvider();
        var noOpService = services.GetService<NoOpService>();
        var hostedService = services.GetService<IHostedService>();

        // assert
        Assert.NotNull(noOpService);
        Assert.NotNull(hostedService);
        Assert.Equal(1, NoOpService.instanceCounter);
    }

    private class NoOpService : IHostedService
    {
        public static int instanceCounter = 0;

        public NoOpService()
        {
            instanceCounter++;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
