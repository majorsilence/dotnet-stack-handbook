using Microsoft.Extensions.Logging.Abstractions;

namespace Examples.Concurrency;

// Runs the listings from "Asynchronous Work and Threads".
public static class Program
{
    public static async Task Main()
    {
        await RunAsyncAwait();
        await RunThreadIsAlive();
        RunThreadJoin();
        await RunLock();
        await RunInterlocked();
        await RunWorkQueue();
        await RunThreadPool();
    }

    // --- Async/await -------------------------------------------------------------
    private static async Task LoadPreviousSettings()
    {
        // the book sleeps 5000ms here; shortened so the examples run in CI
        await Task.Delay(50);
    }

    private static async Task RunAsyncAwait()
    {
        var loadTask = LoadPreviousSettings();

        // Do some other crazy stuff

        await loadTask;
        Console.WriteLine("settings loaded");
    }

    // --- Threads -----------------------------------------------------------------
    private static void ThreadMethod()
    {
        Console.WriteLine("The code in this method is running in its own thread.");
        Thread.Sleep(100);
    }

    private static async Task RunThreadIsAlive()
    {
        var t = new Thread(ThreadMethod);
        t.Start();

        Console.WriteLine("Do other things while waiting for the background thread to finish");

        while (t.IsAlive)
        {
            Console.WriteLine("Alive");
            await Task.Delay(50);
        }

        Console.WriteLine("job completed");
    }

    private static void CountingThreadMethod()
    {
        for (int i = 1; i < 6; i++)
        {
            Console.WriteLine($"background loop count {i}");
            Thread.Sleep(10);
        }
    }

    private static void RunThreadJoin()
    {
        var t = new Thread(CountingThreadMethod);
        t.Start();
        Console.WriteLine("Wait for the background thread to complete");
        t.Join();
        Console.WriteLine("job completed");
    }

    // --- Locks -------------------------------------------------------------------
    private static async Task RunLock()
    {
        var tasks = new List<Task>();
        // .NET 9 and newer: the dedicated Lock type, not a plain object
        var lockObject = new Lock();

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

        foreach (var t in tasks)
        {
            await t;
        }

        Console.WriteLine(count);
    }

    private static async Task RunInterlocked()
    {
        int count = 0;
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j <= 999; j++)
                {
                    Interlocked.Increment(ref count);
                }
            }));
        }

        foreach (var t in tasks)
        {
            await t;
        }

        Console.WriteLine(count);
    }

    // --- In memory work queue ----------------------------------------------------
    private static async Task RunWorkQueue()
    {
        var queue = new WorkQueue();
        var worker = new QueueWorker(queue, NullLogger<QueueWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        var done = new TaskCompletionSource();
        await queue.EnqueueAsync(async token =>
        {
            await Task.Delay(10, token);
            Console.WriteLine("report generated");
        });
        // the try/catch in the worker means this one is logged, not fatal
        await queue.EnqueueAsync(_ => throw new InvalidOperationException("bad work item"));
        await queue.EnqueueAsync(_ =>
        {
            Console.WriteLine("queue still draining after a failed item");
            done.SetResult();
            return Task.CompletedTask;
        });

        await done.Task;
        queue.Complete();
        await worker.StopAsync(CancellationToken.None);
    }

    // --- Thread queue ------------------------------------------------------------
    private static int ExpensiveCalculation(int input) => input * input;

    private static async Task ProcessShowAsync(string show, CancellationToken token)
    {
        await Task.Delay(10, token);
        Console.WriteLine($"processed {show}");
    }

    private static async Task RunThreadPool()
    {
        var work = Task.Run(() => ExpensiveCalculation(7));

        // do other things

        int result = await work;
        Console.WriteLine(result);

        string[] shows = ["Star Trek", "Friends", "Rick and morty"];
        await Parallel.ForEachAsync(
            shows,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (show, token) =>
            {
                await ProcessShowAsync(show, token);
            });

        var background = new Thread(CountingThreadMethod) { IsBackground = true };
        background.Start();
        background.Join();
    }
}
