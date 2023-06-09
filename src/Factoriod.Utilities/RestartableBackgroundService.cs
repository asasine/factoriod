using Microsoft.Extensions.Hosting;

namespace Factoriod.Utilities;

public abstract class RestartableBackgroundService : IHostedService, IDisposable
{
    /// <summary>
    /// A cancellation token source for the currently-running <see cref="backgroundTask"/>.
    /// </summary>
    private CancellationTokenSource? backgroundTaskCts = null;

    /// <summary>
    /// The asynchronous operation which launched the factorio process.
    /// </summary>
    private Task? backgroundTask = null;

    /// <summary>
    /// Called by <see cref="StartAsync(CancellationToken)"/>.
    /// The returned <see cref="Task"/> represents the long-running operation.
    /// </summary>
    /// <param name="stoppingToken">Cancelled by <see cref="StopAsync(CancellationToken)"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the long-running operation.</returns>
    /// <remarks>
    /// This method does not need to be reentrant.
    /// The implementation of <see cref="StartAsync(CancellationToken)"/> does not invoke this method if a previous execution is still ongoing.
    /// </remarks>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Starts the background task.
    /// </summary>
    /// <param name="cancellationToken">A token indicating the starting process should be aborted.</param>
    /// <returns>A task that completes when the background task has been started.</returns>
    /// <remarks>
    /// This method is reentrant and can be called multiple times without starting multiple background processes.
    /// 
    /// <para>
    /// If the background task had previously been started and has since completed, a prior call to <see cref="StopAsync(CancellationToken)"/>
    /// is not necessary.
    /// </para>
    /// </remarks>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (this.backgroundTask != null)
        {
            // previously started
            if (!this.backgroundTask.IsCompleted)
            {
                // not completed, nothing to do
                return Task.CompletedTask;
            }
            else
            {
                // completed, restart it
                this.backgroundTask.Dispose();
            }
        }

        this.backgroundTaskCts?.Dispose();
        this.backgroundTaskCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        this.backgroundTask = ExecuteAsync(this.backgroundTaskCts.Token);

        if (this.backgroundTask.IsCompleted)
        {
            // the task completed synchronously, bubble up its cancellation and failures
            return this.backgroundTask;
        }

        // otherwise, it's running, return a completed task
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the background task.
    /// </summary>
    /// <param name="cancellationToken">A token indicating the stopping process should no longer be graceful.</param>
    /// <returns>A task that completes when the background task has completed or <paramref name="cancellationToken"/> has been cancelled.</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this.backgroundTask == null || this.backgroundTask.IsCompleted)
        {
            this.backgroundTask?.Dispose();
            return;
        }

        try
        {
            this.backgroundTaskCts?.Cancel();
        }
        finally
        {
            // wait for the task to finish, or cancellationToken to trigger
            var tcs = new TaskCompletionSource();
            await Task.WhenAny(this.backgroundTask, tcs.Task.WaitAsync(cancellationToken));
        }
    }

    /// <summary>
    /// Restarts the background task.
    /// This is safe to call even if the background task is not running.
    /// </summary>
    /// <param name="cancellationToken">A token indicating the restart should be aborted.</param>
    /// <returns>A task that completes when the background task has completed and been started again, or <paramref name="cancellationToken"/> has been cancelled.</returns>
    public async Task RestartAsync(CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken);
        await StartAsync(cancellationToken);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (this.backgroundTask != null && this.backgroundTask.IsCompleted)
        {
            this.backgroundTask.Dispose();
        }

        this.backgroundTask = null;
        this.backgroundTaskCts?.Cancel();
        this.backgroundTaskCts = null;
    }
}
