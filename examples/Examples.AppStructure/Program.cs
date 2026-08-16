using Dapper;
using MajorSilence.DataAccess;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Examples.AppStructure;

public static class Program
{
    public static void Main()
    {
        // A real file, not :memory:.  Both repositories open a connection per
        // call, and each :memory: connection gets its own empty database, so the
        // insert and the select would never meet.
        var dbPath = Path.Combine(Path.GetTempPath(), $"handbook-{Guid.NewGuid():N}.db");
        var cnStr = $"Data Source={dbPath}";

        try
        {
            CreateSchema(cnStr);
            RunManualWiring(cnStr);
            RunContainer(cnStr);
            RunEvents();
        }
        finally
        {
            // Microsoft.Data.Sqlite pools connections, so the file stays locked
            // until the pool is cleared.
            SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }
    }

    private static void CreateSchema(string cnStr)
    {
        using var cn = new SqliteConnection(cnStr);
        cn.Execute("CREATE TABLE TheTable (Name TEXT NOT NULL);");
    }

    // --- Repository pattern, wired by hand ---------------------------------------
    private static void RunManualWiring(string cnStr)
    {
        // Our repository layer that will talk to the data source.
        // This could be inject with a dependency injection framework
        var repo = new TestRepo(cnStr);
        var repo2 = new TestRepoNobase(cnStr);

        // Our business class.  Takes an interface and does not care
        // what the actual data source is.
        var inst = new MajorSilence.BusinessStuff.TestStuff(repo);
        inst.DoStuff();

        var inst2 = new MajorSilence.BusinessStuff.TestStuff(repo2);
        inst2.DoStuff();
    }

    // --- The same thing, wired by the container ----------------------------------
    private static void RunContainer(string cnStr)
    {
        var services = new ServiceCollection();

        // map the interface to the implementation
        services.AddSingleton<ITestRepo>(sp => new TestRepo(cnStr));
        services.AddTransient<MajorSilence.BusinessStuff.TestStuff>();

        using var provider = services.BuildServiceProvider();

        // TestStuff needs an ITestRepo.  The container supplies it.
        var inst = provider.GetRequiredService<MajorSilence.BusinessStuff.TestStuff>();
        inst.DoStuff();
    }

    // --- Events ------------------------------------------------------------------
    private static void RunEvents()
    {
        // subscribe using lamba expression
        var x = new Events.BuiltIn.TheExample();
        x.DoSomething += (s, e) =>
        {
            Console.WriteLine("hi, the event has been raised");
        };
        x.TheTest();

        var y = new Events.CustomDelegate.TheExample();
        y.DoSomething += (s, e) => Console.WriteLine("custom delegate event raised");
        y.TheTest();

        var z = new Events.Custom.Publisher();
        z.DoSomething += (s, e) => Console.WriteLine($"custom event args: {e.Value}");
        z.Raise();
    }
}
