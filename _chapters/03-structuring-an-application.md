---
layout: chapter
title: "Structuring an Application"
number: 3
part: 1
examples: Examples.AppStructure
---

The previous chapters were about writing code that works. This one is about arranging it so that it keeps working after a year of changes and can be tested without standing up a database.

The three patterns here reinforce each other. Inversion of control means a class is handed what it depends on rather than constructing it. The repository pattern puts data access behind an interface, which is what gives inversion of control something worth injecting. Events let one part of an application tell others that something happened without knowing who is listening. The chapter closes on packaging code as NuGet, and on testing, which is the payoff for all of it.

## IOC {#ioc}

**Inversion of Control (IOC)** means a class does not create the things it depends on. It is given them instead. **Dependency injection (DI)** is the usual way to do that: dependencies are passed into the constructor, and something else decides which concrete implementation to supply.

This is what makes the interfaces and repository classes shown in the previous sections worth writing. `TestStuff` takes an `ITestRepo` and never learns whether it is talking to SQL Server, SQLite, or a mock in a unit test.

.NET ships a DI container in `Microsoft.Extensions.DependencyInjection`. In an asp.net core app it is already wired up. For a console app or a Winforms app, add the package.

```bash
dotnet add package Microsoft.Extensions.DependencyInjection
```

### Registering and resolving services

Registration maps an interface to the concrete class that implements it. Resolution walks the constructor parameters and builds the whole object graph for you.

```cs
using System;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// map the interface to the implementation
services.AddSingleton<ITestRepo>(sp =>
    new MajorSilence.DataAccess.TestRepo("Data Source=shows.db"));
services.AddTransient<MajorSilence.BusinessStuff.TestStuff>();

using var provider = services.BuildServiceProvider();

// TestStuff needs an ITestRepo.  The container supplies it.
var inst = provider.GetRequiredService<MajorSilence.BusinessStuff.TestStuff>();
inst.DoStuff();
```

Compare that with the manual wiring in the **Repository Pattern** section. For two classes the manual version is fine. Once an application has fifty of them, the container is what keeps `Main` from becoming a wall of `new`.

### Lifetimes

Choosing the wrong lifetime is the most common source of DI bugs, so it is worth being deliberate about it.

- **Transient** - a new instance every time it is requested. The safe default for cheap, stateless classes.
- **Scoped** - one instance per scope. In asp.net core a scope is one http request. This is what `DbContext` should use.
- **Singleton** - one instance for the life of the application. Must be thread safe, since many threads can use it at once.

The rule that catches people out: a singleton must never depend on a scoped service. The singleton is built once and holds onto whatever it was given, so it would keep using the first request's scoped instance forever. The container will throw at startup if you try, provided scope validation is on, which it is by default in development.

### Registering by convention

Registering interfaces one by one is tedious in a large solution. [Scrutor](https://github.com/khellang/Scrutor) scans an assembly and registers everything matching a convention.

```bash
dotnet add package Scrutor
```

```cs
services.Scan(scan => scan
    .FromAssemblyOf<MajorSilence.DataAccess.ITestRepo>()
        .AddClasses(classes => classes.Where(t => t.Name.EndsWith("Repo")))
        .AsImplementedInterfaces()
        .WithScopedLifetime());
```

### Why this makes testing easy

Because `TestStuff` only ever sees `ITestRepo`, a test can hand it a stand-in and assert on what it did, with no database involved. See [mocking](/docs/VbIntroduction/Mocking.html).

```cs
using Moq;
using NUnit.Framework;

[Test]
public void DoStuffInsertsTheName()
{
    var repo = new Mock<ITestRepo>();
    repo.Setup(x => x.GetName()).Returns("The Name");

    var inst = new MajorSilence.BusinessStuff.TestStuff(repo.Object);
    inst.DoStuff();

    repo.Verify(x => x.InsertData("The Name"), Times.Once);
}
```

### Other containers

The built in container covers most needs. Third party containers add features such as property injection, interception, and more advanced conditional registration.

- [Autofac](https://autofac.org/)
- [Lamar](https://jasperfx.github.io/lamar/)

## Repository Pattern

Use the repository pattern to separate your business and data access layers. Makes
it easy to test your business and data layer code separately.

There are different ways to do this. Here are a couple ways.

### Use a base abstract class that is passed a connection

```cs
using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace MajorSilence.DataAccess
{
    public abstract class BaseRepo
    {
        private readonly string cnStr;

        protected BaseRepo(string cnStr)
        {
            this.cnStr = cnStr;
        }

        protected T WithConnection<T>(Func<IDbConnection, T> sqlTransaction)
        {
            using (var connection = new SqliteConnection(cnStr))
            {
                connection.Open();
                return sqlTransaction(connection);
            }
        }

        protected void WithConnection(Action<IDbConnection> sqlTransaction)
        {
            using (var connection = new SqliteConnection(cnStr))
            {
                connection.Open();
                sqlTransaction(connection);
            }
        }

        protected async Task<T> WithConnectionAsync<T>(Func<IDbConnection, Task<T>> sqlTransaction)
        {
            using (var connection = new SqliteConnection(cnStr))
            {
                await connection.OpenAsync();
                return await sqlTransaction(connection);
            }
        }

        protected async Task WithConnectionAsync(Func<IDbConnection, Task> sqlTransaction)
        {
            using (var connection = new SqliteConnection(cnStr))
            {
                await connection.OpenAsync();
                await sqlTransaction(connection);
            }
        }
    }
}
```

And here is the repo class

```cs
using System;
using System.Linq;
using Dapper;

namespace MajorSilence.DataAccess
{
    public interface ITestRepo
    {
        // string? and not string: the table may be empty, and FirstOrDefault
        // returns null when it is.  The signature is where that belongs.
        string? GetName();
        void InsertData(string name);
    }

    public class TestRepo : BaseRepo, ITestRepo
    {
        public TestRepo(string cnStr) : base(cnStr) { }

        public string? GetName()
        {
            return this.WithConnection(cn =>
            {
                return cn.Query<string>("SELECT Name From TheTable LIMIT 1;").FirstOrDefault();
            });
        }

        public void InsertData(string name)
        {
            this.WithConnection(cn =>
            {
                cn.Execute("INSERT INTO TheTable (Name) VALUES (@Name);",
                    new { Name = name });
            });
        }
    }
}
```

### No base abstract. Let individual repository classes do as they please

I generally prefer this way. It is simple.

```cs
using Microsoft.Data.Sqlite;
using System.Linq;
using Dapper;

namespace MajorSilence.DataAccess
{

    public class TestRepoNobase : ITestRepo
    {
        readonly string cnStr;
        public TestRepoNobase(string cnStr)
        {
            this.cnStr = cnStr;
        }

        public string? GetName()
        {
            using (var cn = new SqliteConnection(cnStr))
            {
                return cn.Query<string>("SELECT Name From TheTable LIMIT 1;").FirstOrDefault();
            };
        }

        public void InsertData(string name)
        {
            using (var cn = new SqliteConnection(cnStr))
            {
                cn.Execute("INSERT INTO TheTable (Name) VALUES (@Name);",
                    new { Name = name });
            };
        }
    }
}
```

### Do something with the repository classes

A business class

```cs
using System;
namespace MajorSilence.BusinessStuff
{
    public class TestStuff
    {
        readonly DataAccess.ITestRepo repo;
        public TestStuff(DataAccess.ITestRepo repo)
        {
            this.repo = repo;
        }

        public void DoStuff()
        {
            repo.InsertData("The Name");

            // GetName returns string?, so the empty table case is handled here
            // instead of turning up later as a NullReferenceException.
            string name = repo.GetName() ?? "(no rows)";

            // Do stuff with the name
        }
    }
}
```

Combine everything. Manually initialize our two repository classes and initialize two copies
of our TestStuff class. Our TestStuff never knows what or where the actual data layer is.

Note the file backed connection string. Both repository classes open a fresh connection per
call, and with SQLite a plain `Data Source=:memory:` gives every connection its own private,
empty database - so the insert and the select would not see each other. If you want an in
memory database that survives across connections, use
`Data Source=shows;Mode=Memory;Cache=Shared` and keep one connection open for as long as the
database should live.

TestStuff is now easily tested with tools such as as [moq](/docs/VbIntroduction/Mocking.html).

```cs
using System;

namespace MajorSilence.TestStuff
{
    class Program
    {
        static void Main(string[] args)
        {

            // Our repository layer that will talk to the data source.
            // This could be inject with a dependency injection framework
            var repo = new MajorSilence.DataAccess.TestRepo("Data Source=shows.db");
            var repo2 = new MajorSilence.DataAccess.TestRepoNobase("Data Source=shows.db");

            // Our business class.  Takes an interface and does not care
            // what the actual data source is.
            var inst = new MajorSilence.BusinessStuff.TestStuff(repo);
            inst.DoStuff();

            var inst2 = new MajorSilence.BusinessStuff.TestStuff(repo2);
            inst2.DoStuff();

        }
    }
}
```

## Events

Custom Event and Event Handlers

### Use built in EventHandler

```cs
public class TheExample
{
    public event System.EventHandler DoSomething;

    public void TheTest(){
        // option 1 to raise event
        this.DoSomething?.Invoke(this, new System.EventArgs());

        // option 2 to raise event
        if (DoSomething != null)
        {
            DoSomething(this, new System.EventArgs());
        }
    }
}
```

### Use custom delegate as event hander

```cs
public class TheExample
{
    public delegate void MyCustomEventHandler(object sender, System.EventArgs e);
    public event MyCustomEventHandler DoSomething;

    public void TheTest(){
        // option 1 to raise event
        this.DoSomething?.Invoke(this, new System.EventArgs());

        // option 2 to raise event
        if (DoSomething != null)
        {
            DoSomething(this, new System.EventArgs());
        }
    }
}
```

### Subscribe to the event

```cs
// subscribe using lamba expression

var x = new TheExample();

x.DoSomething += (s,e) => {
    Console.WriteLine("hi, the event has been raised");
};
x.TheTest();
```

### VB example of basic custom events

```vb
Public Class TheExample
    Public Delegate Sub MyCustomEventHandler(ByVal sender As Object, ByVal e As System.EventArgs)
    Public Event DoSomething As MyCustomEventHandler

    Public Sub TheTest()
        RaiseEvent DoSomething(Me, New EventArgs())
    End Sub
End Class
```

Subscribe to the event

```vb
Dim x As New TheExample
AddHandler x.DoSomething, AddressOf EventCallback
x.TheTest()

RemoveHandler x.DoSomething, AddressOf EventCallback

Sub EventCallback(ByVal sender As Object, ByVal e As System.EventArgs)
    Console.WriteLine("Hi, the event has been raised")
End Sub

```

### Create a custom event

Setup a new custom event class inheriting from EventArgs and setup a new delegate.

```cs
public delegate void MyCustomEventHandler(object sender, MyCustomEvent e);

public class MyCustomEvent : System.EventArgs
{
        private string _msg;
        private float _value;

    public MyCustomEvent(string m)
    {
        _msg = m;
        _value = 0;
    }

    public MyCustomEvent(float v)
    {
        _msg = "";
        _value = v;
    }

    public string Message
    {
        get { return _msg; }
    }

    public float Value
    {
        get { return _value; }
    }
}
```

### Use the custom event

```cs
public event MyCustomEventHandler DoSomething;

this.DoSomething?.Invoke(this, new MyCustomEvent(123.95f));
```

## Nuget

Generally using nuget is very simple. Using Visual Studio right click your solution or project and select "Add Nuget Package". Find your package and add it. It is auto added. Any time you now clone your project on a new computer the first time you build your project it will restore your nuget references.

### Create a NuGet Package

Given a .csproj or .vbproj file with a PropertyGroup like the following, add the **GeneratePackageOnBuild**, **PackageProjectUrl**, **Description**, **Authors**, **RepositoryUrl**, **PackageLicenseExpression**, **Version**.

```xml
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
  </PropertyGroup>
```

PropertyGroup that generates a nuget package on build and fills in many useful details.

```xml
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <PackageProjectUrl>https://PLACEHOLDER</PackageProjectUrl>
    <Description>PLACEHOLDER</Description>
    <Authors>PLACEHOLDER</Authors>
    <RepositoryUrl>https://PLACEHOLDER</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <Version>1.0.1</Version>
  </PropertyGroup>
```

### Add NuGet source

The following command will add a nuget source to your computer other than the default. This is good for self hosted nuget servers. Use --store-password-in-clear-text if a mac or linux workstation is being used.

```bash
dotnet nuget add source "https://your.source.url/v3/index.json" -n [Feed Name] -u YourUserName -p YourPassword --store-password-in-clear-text
```

### Check if NuGet Source already Exists

The following powershell script will check if a nuget source already exists on your computer.

```bash
dotnet nuget list source
```

### Start fresh with just nuget.org

```bash
dotnet new nugetconfig
```

## Testing and Coverage

### NUnit and Coverlet

[NUnit](https://nunit.org/) is a fine testing framework for c#, vb and other .net based languages.

The nuget package **NUnit** must be referenced for base NUnit support in a test project, and **NUnit3TestAdapter** is what actually lets the test runner find the tests - leave it out and `dotnet test` reports zero tests without ever saying why. Despite the 3 in its name it is the current adapter for NUnit 4 as well. **NunitXml.TestLogger** produces NUnit format result files for CI systems that expect them. For integration within visual studio and rider **Microsoft.NET.Test.Sdk** should also be added to the test project.   **coverlet.collector** is used to generate the code coverage report.   Note, for large solutions and projects coverlet can add a considerable overhead.

```bash
dotnet add package NUnit
dotnet add package NUnit3TestAdapter
dotnet add package NunitXml.TestLogger
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package coverlet.collector
```

To demonstrate the the nunit testing framework we will work with a contrived example. The test class will test a modified threaded lock example from above.

Within the test class **ComplexAdditionTests** the code will confirm that the calculation works. This is helpful if a developer ever changes the CalculateWithLock method and breaks it. The test will fail and the developer will know that the change causes problems. The test will test the class **ComplexAddition**.

```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class ComplexAdditionTests
{
    [Test]
    public async Task CalculationsCalculatesTest()
    {
        var complexAdds = new ComplexAddition();

        // 10 outer iterations, each adding 1 once per inner value 0..999
        const int expectedResult = 10 * 1000;
        int actualResult = await complexAdds.CalculateWithLock(10, 999);

        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }
}

public class ComplexAddition
{
    public async Task<int> CalculateWithLock(int outerLimit = 10, int innerLimit = 999)
    {
        var tasks = new List<Task>();
		var lockObject = new object();

		int count = 0;
		for (int i = 0; i < outerLimit; i++)
		{
			tasks.Add(Task.Factory.StartNew(() =>
			{
				for (int j = 0; j <= innerLimit; j++)
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

		return count;
    }
}
```

The tests can be run from within visual studios test explorer or from the command line with either **dotnet test**.

```bash
dotnet test
```

To test and collect coverage data run dotnet test with collector arguments.

```bash
dotnet test -c Release YourSolutionFile.sln --collect:"XPlat Code Coverage" --logger:"nunit"
```

Passing extra args example with exclude by file.
```bash
dotnet test -c Release YourSolutionFile.sln --collect:"XPlat Code Coverage" --logger:"nunit" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile='**/File1ToIgnore.cs,**/File2ToIgnore.cs'
```

### Other test frameworks

Unit test frameworks:

- xUnit
- [MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest)

Acceptance testing framework

- [FitNesse](http://docs.fitnesse.org/FrontPage)

BDD (Behavior-driven development) testing

- [Reqnroll](https://reqnroll.net/)
