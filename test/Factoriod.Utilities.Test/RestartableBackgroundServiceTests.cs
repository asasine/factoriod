using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Factoriod.Utilities.Test;

public class RestartableBackgroundServiceTests
{
    [Fact]
    public async Task StartInvokesExecuteAsync()
    {
        using var testService = new TestService();
        await testService.StartAsync(default);
        Assert.Equal(1, testService.executeCounter);
        Assert.Equal(0, testService.taskCompletedCounter);
    }

    [Fact]
    public async Task StopCancelsStoppingToken()
    {
        using var testService = new TestService();
        await testService.StartAsync(default);
        await testService.StopAsync(default);
        Assert.Equal(1, testService.executeCounter);
        Assert.Equal(1, testService.taskCompletedCounter);
    }

    [Fact]
    public async Task MultipleStartsOnlyExecutesOnce()
    {
        using var testService = new TestService();
        await testService.StartAsync(default);
        await testService.StartAsync(default);
        Assert.Equal(1, testService.executeCounter);
        Assert.Equal(0, testService.taskCompletedCounter);
    }

    [Fact]
    public async Task StopOnStoppedTaskDoesNothing()
    {
        using var testService = new TestService();
        await testService.StopAsync(default);
        Assert.Equal(0, testService.executeCounter);
        Assert.Equal(0, testService.taskCompletedCounter);
    }

    [Fact]
    public async Task RestartOnStoppedTaskStarts()
    {
        using var testService = new TestService();
        await testService.RestartAsync(default);
        Assert.Equal(1, testService.executeCounter);
        Assert.Equal(0, testService.taskCompletedCounter);
    }

    [Fact]
    public async Task RestartOnStartedTaskStopsAndStarts()
    {
        using var testService = new TestService();
        await testService.StartAsync(default);
        await testService.RestartAsync(default);
        Assert.Equal(2, testService.executeCounter);
        Assert.Equal(1, testService.taskCompletedCounter);
    }

    [Fact]
    public async Task StopUngracefullyAbortsTask()
    {
        using var testService = new TestService(ignoreStoppingToken: true);
        await testService.StartAsync(default);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await testService.StopAsync(cts.Token);

        // the task should have started but never completed
        Assert.Equal(1, testService.executeCounter);
        Assert.Equal(0, testService.taskCompletedCounter);
    }

    private class TestService : RestartableBackgroundService
    {
        private readonly bool ignoreStoppingToken;
        public int executeCounter = 0;
        public int taskCompletedCounter = 0;

        /// <summary>
        /// Create a <see cref="TestService"/>.
        /// </summary>
        /// <param name="ignoreStoppingToken"><see langword="true"/> to ignore the <see cref="CancellationToken"/> passed to <see cref="ExecuteAsync(CancellationToken)"/>. Defaults to <see langword="false"/></param>
        public TestService(bool ignoreStoppingToken = false)
        {
            this.ignoreStoppingToken = ignoreStoppingToken;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            executeCounter++;
            try
            {
                await Task.Delay(Timeout.Infinite, ignoreStoppingToken ? CancellationToken.None : stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // catch TaskCanceledException to execute the remainder of this method
            }

            taskCompletedCounter++;
        }
    }
}
