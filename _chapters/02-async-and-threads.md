---
layout: chapter
title: "Asynchronous Work and Threads"
number: 2
part: 1
examples: Examples.Concurrency
---

Work that waits and work that computes are different problems, and .NET gives them different tools. Confusing the two is the usual source of both sluggish applications and mysterious data corruption.

Async and await are for waiting: a network call, a database query, a file read. The thread is released while the wait happens rather than sitting idle. Threads and the thread pool are for computing: work that genuinely needs a CPU. This chapter covers both, the locking that becomes necessary the moment two of them touch the same variable, and an in memory work queue for handing a slow job off and answering the caller straight away.

## Async/Await

> [Asynchronous programming](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/async-scenarios)
> . The core of async programming is the Task and Task<T> objects, which model asynchronous operations. They are supported by the async and await keywords. The model is fairly simple in most cases:
> For I/O-bound code, you await an operation that returns a Task or Task<T> inside of an async method.
> For CPU-bound code, you await an operation that is started on a background thread with the Task.Run method.

Async and await provides a way for more efficient use of threads. When a task is run it can be awaited later while doing more work while waiting.

Simple async/await example:

```vb
Private Async Function LoadPreviousSettings() As Task
	Await Task.Delay(5000)
End Function

Dim loadTask As Task = LoadPreviousSettings()

' Do some other crazy stuff

Await loadTask
```

```cs
private async Task LoadPreviousSettings()
{
	await Task.Delay(5000);
}

var loadTask = LoadPreviousSettings();

// Do some other crazy stuff

await loadTask;
```

The async and await pattern makes asynchronous programming easier and feels more like sequential development. Good places for async/await is I/O bound work such as when making network calls. Much of the time is spent waiting for a response and the thread could be doing other work while waiting. Network calls such as database connections, commands, updates, inserts, selects, deletes, and stored procedure and functions executions should be run with async and await pattern.

Another place async/await should be used is when making http calls. The example below demonstrates using async/await when using HttpClient to download a web site front page. In an asp.net core application IHttpClientFactory should be used to create an HttpClient.

```cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;

public static async Task Main()
{
    // normally disposable objects should be disposed.
    // HttpClient is a special case and its norm is
    // that it should not be disposed until the program terminates
	using var client = new HttpClient();
    // add all tasks to a list and later await them.
    var tasks = new List<Task<string>>();
    Stopwatch stopWatch = new Stopwatch();
    stopWatch.Start();
    for(int i=0; i<10; i++)
    {
        var instDownloader = new Downloader();
        tasks.Add(instDownloader.DownloadSiteAsync(client, "https://majorsilence.com"));
    }
    Console.WriteLine("majorsilence.com is being downloaded 10 times.  Waiting...");
    foreach(var t in tasks)
    {
        string html = await t;
        Console.WriteLine(html.Substring(0, 100));
    }
    stopWatch.Stop();
    TimeSpan ts = stopWatch.Elapsed;
    Console.WriteLine($"Code Downloaded in {ts.TotalMilliseconds} Milliseconds");

    // sequential async calls
    Console.WriteLine("start sequential async calls to download majorsilence.com.  Waiting...");
    stopWatch.Restart();
    for(int i=0; i<10; i++)
    {
        var instDownloader = new Downloader();
        string html = await instDownloader.DownloadSiteAsync(client, "https://majorsilence.com");
        Console.WriteLine(html.Substring(0, 100));
    }
    stopWatch.Stop();
    TimeSpan ts2 = stopWatch.Elapsed;
    Console.WriteLine($"Sequential Code Downloaded in {ts2.TotalMilliseconds} Milliseconds");
}

public class Downloader{
    public async Task<string> DownloadSiteAsync(HttpClient httpClient,
        string url,
        System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };

        // proceed past user agent sniffing
        request.Headers.Add("User-Agent", "Mozilla/5.0 (X11; CrOS x86_64 14541.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");

        HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}
```

## Threads

> [In computer science, a thread of execution is the smallest sequence of programmed instructions that can be managed independently by a scheduler, which is typically a part of the operating system](<https://en.wikipedia.org/wiki/Thread_(computing)>).

Dot net provides the [Thread](https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread?view=net-10.0) class.

Here is an example that starts a background tasks and checks every 500 millisecond if it is complete using the IsAlive property. If the background thread is still working it continues its work inside a while loop.

```cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main()
    {
        var t = new Thread(ThreadMethod);
        t.Start();

        Console.WriteLine("Do other things while waiting for the background thread to finish");

        while(t.IsAlive){
            Console.WriteLine("Alive");
            await Task.Delay(500);
        }

        Console.WriteLine("job completed");
    }

    static void ThreadMethod(){
        Console.WriteLine("The code in this method is running in its own thread.");
        Console.WriteLine("Sleep the thread 5000 milliseconds to demonstrate the main thread keeps working.");
        Thread.Sleep(5000);
    }
}
```

This example starts a thread and does no work. The main thread stops work and waits for the background thread to complete using the Join method.

```cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main()
    {
        var t = new Thread(ThreadMethod);
        t.Start();
        Console.WriteLine("Wait for the background thread to complete");
        t.Join();
        Console.WriteLine("job completed");
    }

    static void ThreadMethod(){
        Console.WriteLine("The code in this method is running in its own thread.");
        for(int i = 1; i< 6; i++){
            Console.WriteLine($"background loop count {i}");
            Thread.Sleep(500);
        }
    }
}
```

### Locks

If more than one thread or task is updating a variable you should lock the variable as necessary.

The example below create multiple tasks that all update the same "count" variable.
As you can see it locks the variable before updating it.

```vb
Dim tasks As New List(Of Task)
Dim lockObject As New Object()

Dim count As Integer = 0

For i As Integer = 0 To 9
	tasks.Add(Task.Factory.StartNew(Sub()
		For j As Integer = 0 To 999
			SyncLock lockObject
				count = count + 1
			End SyncLock
		Next
	End Sub))
Next

For Each t In tasks
	Await t
Next

System.Console.WriteLine(count)
```

```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main()
    {
        var tasks = new List<Task>();
		var lockObject = new object();

		int count = 0;
		for (int i = 0; i < 10; i++)
		{
			tasks.Add(Task.Factory.StartNew(() =>
			{
				for (int j = 0; j <= 999; j++)
				{
					lock (lockObject)
                    {
						count = count + 1;
					}
				}
			}));
		}

		foreach(var t in tasks)
		{
			await t;
		}

		Console.WriteLine(count);
    }
}
```

On .NET 9 and newer prefer the dedicated [System.Threading.Lock](https://learn.microsoft.com/en-us/dotnet/api/system.threading.lock?view=net-10.0) type over locking on a plain `object`. The c# compiler recognises it and emits the faster `Lock.EnterScope` path. The only change needed is the declaration.

```cs
// was: var lockObject = new object();
var lockObject = new System.Threading.Lock();

lock (lockObject)
{
    count = count + 1;
}
```

For the specific case of incrementing a counter, skip the lock entirely and use [Interlocked](https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked?view=net-10.0), which is cheaper.

```cs
System.Threading.Interlocked.Increment(ref count);
```

## In Memory Work Queue

Sometimes work should be accepted now and performed later, without involving Redis or any other external broker. A web request that triggers a slow report, or a desktop app that queues uploads, both want the same thing: hand the item off, return immediately, and let a background worker drain the queue.

The two approaches below are both in process. If the application restarts, anything still queued is lost. When that is unacceptable, use a durable queue such as the Redis one shown in the **Work and Message Queue with Redis** section, or a library such as [Hangfire](https://www.hangfire.io/).

### Task Queue

`System.Threading.Channels` is the modern way to do this. A channel is a thread safe producer/consumer queue that supports async reads, so a consumer waits without burning a thread.

Set a bounded capacity. An unbounded queue will happily grow until the process runs out of memory when producers outpace the consumer.

```cs
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
}
```

The consumer loop. In asp.net core this belongs in a `BackgroundService`, registered with `services.AddHostedService<QueueWorker>()` and the queue itself as a singleton.

```cs
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
```

Queueing work from a controller then costs one line, and the caller gets its response straight away.

```cs
await _queue.EnqueueAsync(async token =>
{
    await _reportBuilder.GenerateAsync(reportId, token);
});
```

The try/catch around `workItem` is the part people leave out. Without it, one unhandled exception ends the `await foreach` loop and the queue silently stops draining for the rest of the process lifetime.

### Thread Queue

If the work is CPU bound rather than I/O bound, running it on the thread pool is usually all that is required. `Task.Run` queues the delegate to the pool, which already manages a pool of threads for you.

```cs
var work = Task.Run(() => ExpensiveCalculation(input));

// do other things

int result = await work;
```

For CPU bound work over a collection, `Parallel.ForEachAsync` limits how many run at once, which avoids swamping the pool.

```cs
await Parallel.ForEachAsync(
    shows,
    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
    async (show, token) =>
    {
        await ProcessShowAsync(show, token);
    });
```

Dedicated `Thread` objects, as shown in the **Threads** section, are worth the trouble only for long running work that should not occupy a pool thread for minutes at a time. In that case set `IsBackground = true` so the thread does not keep the process alive at shutdown.

```cs
var t = new Thread(ThreadMethod) { IsBackground = true };
t.Start();
```
