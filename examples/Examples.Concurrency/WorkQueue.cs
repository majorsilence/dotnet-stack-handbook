using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Examples.Concurrency;

// The in memory work queue from "Asynchronous Work and Threads".
public class WorkQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _channel =
        Channel.CreateBounded<Func<CancellationToken, Task>>(
            new BoundedChannelOptions(capacity: 100)
            {
                // block the producer rather than dropping work
                FullMode = BoundedChannelFullMode.Wait
            });

    public async Task EnqueueAsync(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _channel.Writer.WriteAsync(workItem);
    }

    public IAsyncEnumerable<Func<CancellationToken, Task>> ReadAllAsync(
        CancellationToken cancellationToken) =>
            _channel.Reader.ReadAllAsync(cancellationToken);

    // Not in the book's listing.  The demo needs the consumer loop to end, and a
    // channel that is never completed never ends.
    public void Complete() => _channel.Writer.Complete();
}

public class QueueWorker : BackgroundService
{
    private readonly WorkQueue _queue;
    private readonly ILogger<QueueWorker> _logger;

    public QueueWorker(WorkQueue queue, ILogger<QueueWorker> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                // one bad work item must not kill the worker
                _logger.LogError(ex, "Work item failed");
            }
        }
    }
}
