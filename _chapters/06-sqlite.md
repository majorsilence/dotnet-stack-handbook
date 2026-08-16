---
layout: chapter
title: "SQLite"
number: 6
part: 3
examples: Examples.Data
---

SQLite is a database that is a file. There is no server to install, no port to open, no user to create; an application opens the file and starts querying. That single property makes it the right choice far more often than people expect: desktop application storage, a cache, test fixtures, and small web applications all work well on it.

This chapter shows the same small task three ways - raw ADO.NET, Dapper, and Entity Framework Core - because those three layers reappear for every other database in this part of the book. Seeing them against the simplest possible store makes the differences between them easy to see. [Data Access in .NET](10-data-access.html) covers each in more depth.

## SQLite

[SQLite](https://www.sqlite.org/) is a lightweight, serverless, self-contained SQL database engine. It stores the entire database as a single file on disk, requires no separate server process, and is included in .NET by default. SQLite is ideal for development, prototyping, desktop, mobile, and small-to-medium web applications.

**Why use SQLite for new projects?**

- **Zero configuration:** No server setup or management required.
- **Easy to use:** Simple file-based deployment—just copy the database file.
- **Reliable and fast:** ACID-compliant and performant for most workloads.
- **Portable:** Works across platforms (Windows, Linux, macOS).
- **Scalable for prototyping:** Start with SQLite, then migrate to a larger DBMS, PostgreSQL, if/when needed.

See [Why you should probably be using SQLite](https://www.epicweb.dev/why-you-should-probably-be-using-sqlite).

## C# Examples

**Install the NuGet package:**

```bash
dotnet add package Microsoft.Data.Sqlite
```

**Create and query a database:**

```cs
using Microsoft.Data.Sqlite;

var connectionString = "Data Source=tvshows.db";
using var connection = new SqliteConnection(connectionString);
connection.Open();

// Create table
var createCmd = connection.CreateCommand();
createCmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS TvShows (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        ShowName TEXT NOT NULL,
        Rating REAL
    );";
createCmd.ExecuteNonQuery();

// Insert data
var insertCmd = connection.CreateCommand();
insertCmd.CommandText = "INSERT INTO TvShows (ShowName, Rating) VALUES ($name, $rating);";
insertCmd.Parameters.AddWithValue("$name", "Friends");
insertCmd.Parameters.AddWithValue("$rating", 4.8);
insertCmd.ExecuteNonQuery();

// Query data
var selectCmd = connection.CreateCommand();
selectCmd.CommandText = "SELECT Id, ShowName, Rating FROM TvShows;";
using var reader = selectCmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"{reader.GetInt32(0)}: {reader.GetString(1)} ({reader.GetDouble(2)})");
}
```

**Note:** For more advanced scenarios, consider using [Dapper](https://github.com/DapperLib/Dapper) or Entity Framework Core with SQLite as the provider.

## SQLite with Dapper

**Install NuGet packages:**

```bash
dotnet add package Dapper
dotnet add package Microsoft.Data.Sqlite
```

**Example: Querying SQLite with Dapper**

```cs
using System;
using System.Collections.Generic;
using Dapper;
using Microsoft.Data.Sqlite;

public class TvShow
{
    public int Id { get; set; }
    public string ShowName { get; set; }
    public double Rating { get; set; }
}

public class Example
{
    public IEnumerable<TvShow> GetShows()
    {
        using var conn = new SqliteConnection("Data Source=tvshows.db");
        conn.Open();
        return conn.Query<TvShow>("SELECT Id, ShowName, Rating FROM TvShows WHERE Rating > @minRating", new { minRating = 4.0 });
    }
}
```

## SQLite with Entity Framework Core

**Install NuGet packages:**

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

**Example: DbContext and Model**

```cs
using Microsoft.EntityFrameworkCore;

public class TvShow
{
    public int Id { get; set; }
    public string ShowName { get; set; }
    public double Rating { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<TvShow> TvShows { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=tvshows.db");
}

// Usage
using var db = new AppDbContext();
db.TvShows.Add(new TvShow { ShowName = "Friends", Rating = 4.8 });
db.SaveChanges();

var highRated = db.TvShows.Where(t => t.Rating > 4.0).ToList();
```
