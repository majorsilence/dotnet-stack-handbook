---
layout: chapter
title: "Data Access in .NET"
number: 10
part: 3
examples: Examples.Data
---

## DbConnection

A `DbConnection` represents an open connection to a database. It is the base class for database-specific connection classes like `SqlConnection` (SQL Server), `NpgsqlConnection` (PostgreSQL), and `SqliteConnection` (SQLite).

**Example: Using DbConnection with SQL Server**

```cs
using System.Data.Common;
using Microsoft.Data.SqlClient;

string connectionString = "Server=localhost;Database=SqlPlayground;User Id=sa;Password=yourpassword;";
using DbConnection conn = new SqlConnection(connectionString);
conn.Open();

using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM TvShows";
var count = cmd.ExecuteScalar();
Console.WriteLine($"Number of TV shows: {count}");
```

**Example: Using DbConnection with PostgreSQL**

```cs
using System.Data.Common;
using Npgsql;

string connectionString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb";
using DbConnection conn = new NpgsqlConnection(connectionString);
conn.Open();

using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM tv_shows";
var count = cmd.ExecuteScalar();
Console.WriteLine($"Number of TV shows: {count}");
```

**Example: Using DbConnection with SQLite**

```cs
using System.Data.Common;
using Microsoft.Data.Sqlite;

string connectionString = "Data Source=tvshows.db";
using DbConnection conn = new SqliteConnection(connectionString);
conn.Open();

using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT COUNT(*) FROM TvShows";
var count = cmd.ExecuteScalar();
Console.WriteLine($"Number of TV shows: {count}");
```

**Note:** Always dispose connections (use `using` or `await using` for async) to free resources.

## DbCommand

A `DbCommand` represents a SQL statement or stored procedure to execute against a database. It is the base class for provider-specific commands like `SqlCommand` (SQL Server), `NpgsqlCommand` (PostgreSQL), and `SqliteCommand` (SQLite).

**Example: Using DbCommand with SQL Server**

```cs
using System.Data.Common;
using Microsoft.Data.SqlClient;

string connectionString = "Server=localhost;Database=SqlPlayground;User Id=sa;Password=yourpassword;";
using DbConnection conn = new SqlConnection(connectionString);
conn.Open();

using DbCommand cmd = conn.CreateCommand();
cmd.CommandText = "SELECT ShowName, Rating FROM TvShows WHERE Rating > @minRating";
var param = cmd.CreateParameter();
param.ParameterName = "@minRating";
param.Value = 4.0m;
cmd.Parameters.Add(param);

using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"{reader.GetString(0)} ({reader.GetDecimal(1)})");
}
```

**Example: Using DbCommand with PostgreSQL**

```cs
using System.Data.Common;
using Npgsql;

string connectionString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb";
using DbConnection conn = new NpgsqlConnection(connectionString);
conn.Open();

using DbCommand cmd = conn.CreateCommand();
cmd.CommandText = "SELECT show_name, rating FROM tv_shows WHERE rating > @minRating";
var param = cmd.CreateParameter();
param.ParameterName = "@minRating";
param.Value = 4.0m;
cmd.Parameters.Add(param);

using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"{reader.GetString(0)} ({reader.GetDecimal(1)})");
}
```

**Example: Using DbCommand with SQLite**

```cs
using System.Data.Common;
using Microsoft.Data.Sqlite;

string connectionString = "Data Source=tvshows.db";
using DbConnection conn = new SqliteConnection(connectionString);
conn.Open();

using DbCommand cmd = conn.CreateCommand();
cmd.CommandText = "SELECT ShowName, Rating FROM TvShows WHERE Rating > $minRating";
var param = cmd.CreateParameter();
param.ParameterName = "$minRating";
param.Value = 4.0;
cmd.Parameters.Add(param);

using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"{reader.GetString(0)} ({reader.GetDouble(1)})");
}
```

**Note:**  
- Always use parameters to avoid SQL injection.
- Use `ExecuteReader()` for queries, `ExecuteNonQuery()` for inserts/updates/deletes, and `ExecuteScalar()` for single-value results.
- Dispose commands and readers properly (using `using` statements).

## DataAdapters

A `DataAdapter` acts as a bridge between a `DataSet` and a database, allowing you to fill in-memory tables and update the database with changes. It is commonly used in ADO.NET for disconnected data access.

**Example: Using SqlDataAdapter with SQL Server**

```cs
using System.Data;
using Microsoft.Data.SqlClient;

string connectionString = "Server=localhost;Database=SqlPlayground;User Id=sa;Password=yourpassword;";
using var conn = new SqlConnection(connectionString);
using var adapter = new SqlDataAdapter("SELECT * FROM TvShows", conn);

var dataSet = new DataSet();
adapter.Fill(dataSet, "TvShows");

// Access data
foreach (DataRow row in dataSet.Tables["TvShows"].Rows)
{
    Console.WriteLine($"{row["ShowName"]} ({row["Rating"]})");
}
```

**Example: Updating Data with SqlDataAdapter**

```cs
using System.Data;
using Microsoft.Data.SqlClient;

string connectionString = "Server=localhost;Database=SqlPlayground;User Id=sa;Password=yourpassword;";
using var conn = new SqlConnection(connectionString);
using var adapter = new SqlDataAdapter("SELECT * FROM TvShows", conn);

// Auto-generate commands for update/insert/delete
var builder = new SqlCommandBuilder(adapter);

var dataSet = new DataSet();
adapter.Fill(dataSet, "TvShows");

// Modify data in-memory
var table = dataSet.Tables["TvShows"];
table.Rows[0]["Rating"] = 5.0m;

// Push changes back to the database
adapter.Update(dataSet, "TvShows");
```

**Example: Using SQLiteDataAdapter with SQLite**

`Microsoft.Data.Sqlite` does not ship a `DataAdapter`. The older `System.Data.SQLite` provider does.

```cs
using System.Data;
using System.Data.SQLite;

string connectionString = "Data Source=tvshows.db";
using var conn = new SQLiteConnection(connectionString);
using var adapter = new SQLiteDataAdapter("SELECT * FROM TvShows", conn);

var dataSet = new DataSet();
adapter.Fill(dataSet, "TvShows");
```

If you are already using `Microsoft.Data.Sqlite` and only need a `DataTable`, load one from the reader instead of pulling in a second provider.

```cs
using System.Data;
using Microsoft.Data.Sqlite;

using var conn = new SqliteConnection("Data Source=tvshows.db");
conn.Open();

using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT * FROM TvShows";

var table = new DataTable();
using var reader = cmd.ExecuteReader();
table.Load(reader);
```

**Notes:**
- DataAdapters are best for simple, disconnected scenarios.
- For large-scale or modern applications, consider using ORMs like Dapper or Entity Framework.
- Always dispose connections and adapters properly.

## DbTransaction

A `DbTransaction` represents a database transaction, allowing you to execute multiple operations as a single unit of work. If any operation fails, you can roll back all changes to maintain data integrity.

**Example: Using DbTransaction with SQL Server**

```cs
using System.Data.Common;
using Microsoft.Data.SqlClient;

string connectionString = "Server=localhost;Database=SqlPlayground;User Id=sa;Password=yourpassword;";
using DbConnection conn = new SqlConnection(connectionString);
conn.Open();

using var transaction = conn.BeginTransaction();
try
{
    using var cmd1 = conn.CreateCommand();
    cmd1.Transaction = transaction;
    cmd1.CommandText = "INSERT INTO TvShows (ShowName, ShowLength, Summary, Rating, Episode, ParentalGuide) VALUES (@name, @length, @summary, @rating, @episode, @guide)";
    cmd1.Parameters.Add(new SqlParameter("@name", "New Show"));
    cmd1.Parameters.Add(new SqlParameter("@length", 1200));
    cmd1.Parameters.Add(new SqlParameter("@summary", "A new show summary"));
    cmd1.Parameters.Add(new SqlParameter("@rating", 4.5m));
    cmd1.Parameters.Add(new SqlParameter("@episode", "1x01"));
    cmd1.Parameters.Add(new SqlParameter("@guide", "PG"));
    cmd1.ExecuteNonQuery();

    using var cmd2 = conn.CreateCommand();
    cmd2.Transaction = transaction;
    cmd2.CommandText = "UPDATE TvShows SET Rating = Rating + 0.1 WHERE ShowName = @name";
    cmd2.Parameters.Add(new SqlParameter("@name", "New Show"));
    cmd2.ExecuteNonQuery();

    transaction.Commit();
    Console.WriteLine("Transaction committed.");
}
catch
{
    transaction.Rollback();
    Console.WriteLine("Transaction rolled back.");
}
```

**Example: Using DbTransaction with PostgreSQL**

```cs
using System.Data.Common;
using Npgsql;

string connectionString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb";
using DbConnection conn = new NpgsqlConnection(connectionString);
conn.Open();

using var transaction = conn.BeginTransaction();
try
{
    using var cmd = conn.CreateCommand();
    cmd.Transaction = transaction;
    cmd.CommandText = "INSERT INTO tv_shows (show_name, rating) VALUES (@name, @rating)";
    cmd.Parameters.Add(new NpgsqlParameter("@name", "Another Show"));
    cmd.Parameters.Add(new NpgsqlParameter("@rating", 4.2m));
    cmd.ExecuteNonQuery();

    transaction.Commit();
}
catch
{
    transaction.Rollback();
}
```

**Example: Using DbTransaction with SQLite**

```cs
using System.Data.Common;
using Microsoft.Data.Sqlite;

string connectionString = "Data Source=tvshows.db";
using DbConnection conn = new SqliteConnection(connectionString);
conn.Open();

using var transaction = conn.BeginTransaction();
try
{
    using var cmd = conn.CreateCommand();
    cmd.Transaction = transaction;
    cmd.CommandText = "UPDATE TvShows SET Rating = Rating + 0.1 WHERE ShowName = $name";
    // conn is declared as DbConnection, so cmd.Parameters is a
    // DbParameterCollection, which has no AddWithValue.  CreateParameter is the
    // provider neutral equivalent.  Declare conn as SqliteConnection instead and
    // AddWithValue is available again.
    var nameParam = cmd.CreateParameter();
    nameParam.ParameterName = "$name";
    nameParam.Value = "Friends";
    cmd.Parameters.Add(nameParam);
    cmd.ExecuteNonQuery();

    transaction.Commit();
}
catch
{
    transaction.Rollback();
}
```

**Notes:**
- Always associate commands with the transaction (`cmd.Transaction = transaction`).
- Use `Commit()` to save changes or `Rollback()` to undo on error.
- Transactions help ensure data consistency and integrity.
- For async code, use `BeginTransactionAsync()`, `CommitAsync()`, and `RollbackAsync()`.

## ORM - Dapper

```bash
dotnet add package Dapper
```

```cs
using Microsoft.Data.SqlClient;
using Dapper;

await cn.OpenAsync();
var shows = await cn.QueryAsync<TvShow>("select * from TvShows");

public class TvShow
{
    public long Id {get; init;}
    public string ShowName {get; init;}
    public int ShowLength {get; init;}
    public string Summary {get; init;}
    public decimal Rating {get; init;}
    public string Episode {get; init;}
    public string ParentalGuide {get; init;}
}
```

## ORM - Entity Framework

Entity Framework Core (EF Core) is a modern, open-source, object-database mapper for .NET. It enables developers to work with databases using .NET objects, eliminating most of the data-access code typically required. EF Core supports LINQ queries, change tracking, updates, and schema migrations across multiple database providers.

**Example: Basic Usage with a DbContext and Model**

```cs
using Microsoft.EntityFrameworkCore;

public class TvShow
{
    public int Id { get; set; }
    public string ShowName { get; set; }
    public decimal Rating { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<TvShow> TvShows { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer("YourConnectionStringHere");
}

// Usage
using var db = new AppDbContext();
db.TvShows.Add(new TvShow { ShowName = "Friends", Rating = 4.8m });
db.SaveChanges();

var highRated = db.TvShows.Where(t => t.Rating > 4.0m).ToList();
```

### Entity Framework: Raw SQL Queries with FromSql and FromSqlInterpolated

Entity Framework Core allows you to execute SQL queries using the `FromSql` and `FromSqlInterpolated` methods. These methods are safe against SQL injection because they always treat parameter values as SQL parameters, not as part of the SQL command text.

**Example: Using FromSql with Parameters**

```cs
using Microsoft.EntityFrameworkCore;

var minRating = 4.0m;
var shows = db.TvShows
    .FromSql($"SELECT * FROM TvShows WHERE Rating > {minRating}")
    .ToList();
```

**Example: Using FromSqlInterpolated**

```cs
var showName = "Friends";
var result = db.TvShows
    .FromSqlInterpolated($"SELECT * FROM TvShows WHERE ShowName = {showName}")
    .ToList();
```

**Note:**  
- These methods can only be used on queries that return entity types (not arbitrary projections).

For more details, see the [official documentation](https://learn.microsoft.com/en-us/ef/core/querying/raw-sql).

### Entity Framework Core: Disabling Change Tracking

By default, EF Core tracks changes to entities for automatic updates. For read-only scenarios, you can disable change tracking to improve performance using `.AsNoTracking()`.

**Example:**

```cs
using Microsoft.EntityFrameworkCore;

public class TvShow
{
    public int Id { get; set; }
    public string ShowName { get; set; }
    public decimal Rating { get; set; }
}

public class AppDbContext : DbContext
{
    public DbSet<TvShow> TvShows { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlServer("YourConnectionStringHere");
}

// Usage: Query with change tracking disabled
using var db = new AppDbContext();
var shows = db.TvShows
    .AsNoTracking()
    .Where(t => t.Rating > 4.0m)
    .ToList();
```

Use `.AsNoTracking()` for queries where you do not intend to update the returned entities.

## Database Migrations - Entity Framework Core

Entity Framework Core supports code-based migrations to evolve your database schema alongside your models. Migrations are tracked in code and can be applied to the database as needed.

### 1. Add EF Core Tools

Install the EF Core CLI tools if not already present:

```bash
dotnet tool install --global dotnet-ef
```

Add the EF Core packages to your project:

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 2. Create a Migration

After defining or updating your `DbContext` and models, create a migration:

```bash
dotnet ef migrations add InitialCreate
```

This generates a migration file in the `Migrations` folder.

### 3. Apply the Migration

Update the database to apply the migration:

```bash
dotnet ef database update
```

### 4. Example Migration Class

A generated migration might look like:

```cs
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TvShows",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ShowName = table.Column<string>(nullable: false),
                Rating = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TvShows", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TvShows");
    }
}
```

### 5. Further Changes

To modify the schema, update your models and repeat the `dotnet ef migrations add` and `dotnet ef database update` steps.

For more, see the [official EF Core migrations documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/).

## Database Migration - FluentMigrator

[FluentMigrator](https://fluentmigrator.github.io/) is a migration framework for .NET that enables you to define database schema changes in C# using a fluent, expressive API. It supports versioned migrations, rollbacks, and can execute both fluent and raw SQL commands.

### Example: Creating a Table with Fluent Syntax

```cs
using FluentMigrator;

[Migration(2023040701)]
public class CreateTvShowsTable : Migration
{
    public override void Up()
    {
        Create.Table("TvShows")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("ShowName").AsString(50).NotNullable()
            .WithColumn("ShowLength").AsInt32().NotNullable()
            .WithColumn("Rating").AsDecimal(18,2).Nullable();
    }

    public override void Down()
    {
        Delete.Table("TvShows");
    }
}
```

### Example: Executing Raw SQL in a Migration

```cs
using FluentMigrator;

[Migration(2023040702)]
public class InsertSampleData : Migration
{
    public override void Up()
    {
        Execute.Sql("INSERT INTO TvShows (ShowName, ShowLength, Rating) VALUES ('Friends', 1380, 4.8)");
    }

    public override void Down()
    {
        Execute.Sql("DELETE FROM TvShows WHERE ShowName = 'Friends'");
    }
}
```

### Running Migrations

To run migrations, use the FluentMigrator CLI or integrate it into your build pipeline:

```bash
dotnet tool install -g FluentMigrator.DotNet.Cli

fluentmigrator migrate --assembly path/to/Your.Migrations.dll --provider sqlserver --connection "Server=.;Database=YourDb;Trusted_Connection=True;"
```

## Transactions and Isolation Levels

**Transaction isolation levels** determine how and when the changes made by one transaction become visible to other concurrent transactions. They help balance data consistency with system performance and concurrency.

### Common Isolation Levels

| Isolation Level   | Dirty Reads | Non-Repeatable Reads | Phantom Reads | Supported By                |
|-------------------|:-----------:|:--------------------:|:-------------:|-----------------------------|
| Read Uncommitted  |     Yes     |         Yes          |     Yes       | SQL Server, SQLite          |
| Read Committed    |     No      |         Yes          |     Yes       | SQL Server, PostgreSQL, SQLite* |
| Repeatable Read   |     No      |         No           |     Yes       | SQL Server, PostgreSQL      |
| Serializable      |     No      |         No           |     No        | SQL Server, PostgreSQL, SQLite |
| Snapshot          |     No      |         No           |     No*       | SQL Server, PostgreSQL†     |

\* SQLite uses a simplified model; see notes below.  
† PostgreSQL implements snapshot isolation as its default for `REPEATABLE READ`.

### Isolation Level Descriptions

- **Read Uncommitted**: Allows reading uncommitted changes ("dirty reads") from other transactions. Fastest, but least safe.
- **Read Committed**: Only reads data that has been committed. Prevents dirty reads, but non-repeatable and phantom reads are possible. Default in SQL Server and PostgreSQL.
- **Repeatable Read**: Ensures that if a row is read twice in the same transaction, it will not change. Prevents dirty and non-repeatable reads, but phantom reads can still occur.
- **Serializable**: Highest isolation; transactions are completely isolated from each other. Prevents dirty, non-repeatable, and phantom reads. May reduce concurrency.
- **Snapshot**: Each transaction sees a snapshot of the data as it was at the start of the transaction. Prevents dirty and non-repeatable reads, and usually phantom reads.

### Example: Setting Isolation Level in SQL

**SQL Server:**
```sql
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRANSACTION;

SELECT * FROM TvShows WHERE Rating > 4.0;

-- ... do work ...

COMMIT TRANSACTION;
```

**PostgreSQL:**
```sql
BEGIN TRANSACTION ISOLATION LEVEL SERIALIZABLE;

SELECT * FROM tv_shows WHERE rating > 4.0;

-- ... do work ...

COMMIT;
```

**SQLite:**
SQLite supports `DEFERRED`, `IMMEDIATE`, and `EXCLUSIVE` transactions, but you can simulate isolation levels:

```sql
BEGIN IMMEDIATE TRANSACTION;

SELECT * FROM TvShows WHERE Rating > 4.0;

-- ... do work ...

COMMIT;
```
- By default, SQLite is closest to SERIALIZABLE, but with some caveats due to its file-based locking.

### Example: Setting Isolation Level in C#

**SQL Server:**
```cs
using (var conn = new SqlConnection(connectionString))
{
    conn.Open();
    using (var tran = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
    {
        // All commands here use the specified isolation level
        // ...
        tran.Commit();
    }
}
```

**PostgreSQL:**
```cs
using (var conn = new NpgsqlConnection(connectionString))
{
    conn.Open();
    using (var tran = conn.BeginTransaction(System.Data.IsolationLevel.RepeatableRead))
    {
        // All commands here use the specified isolation level
        // ...
        tran.Commit();
    }
}
```

**SQLite:**
```cs
using (var conn = new SqliteConnection(connectionString))
{
    conn.Open();
    using (var tran = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
    {
        // All commands here use the specified isolation level
        // ...
        tran.Commit();
    }
}
```
> Note: SQLite only supports `Serializable` and `Read Uncommitted` isolation levels. `Read Committed` is emulated by default.

### Summary Table

| Isolation Level   | Dirty Reads | Non-Repeatable Reads | Phantom Reads | SQL Server | PostgreSQL | SQLite   |
|-------------------|:-----------:|:--------------------:|:-------------:|:----------:|:----------:|:--------:|
| Read Uncommitted  |     Yes     |         Yes          |     Yes       |    Yes     |    No      |   Yes    |
| Read Committed    |     No      |         Yes          |     Yes       |    Yes     |   Yes*     |  Emulated|
| Repeatable Read   |     No      |         No           |     Yes       |    Yes     |    Yes     |   No     |
| Serializable      |     No      |         No           |     No        |    Yes     |    Yes     |   Yes    |
| Snapshot          |     No      |         No           |     No*       |    Yes     |   Yes†     |   No     |

\* PostgreSQL's default is `Read Committed`, but its `Repeatable Read` is implemented as snapshot isolation.  
† PostgreSQL's `Repeatable Read` is snapshot isolation; true `Serializable` is stricter.

**Tip:** Choose the lowest isolation level that meets your consistency requirements to maximize performance and concurrency.

## SQL Database Backup

Use SqlConnection and SqlCommand to create a bak copy only backup of a database.

```cs
using Microsoft.Data.SqlClient;

public async Task Backup(string connection, string saveFile,
    TimeSpan timeout)
{
    string backupDir = System.IO.Path.GetDirectoryName(saveFile);
    if (System.IO.Directory.Exists(backupDir) == false)
    {
        System.IO.Directory.CreateDirectory(backupDir);
    }

    if (System.IO.File.Exists(saveFile)){
        System.IO.File.Delete(saveFile);
    }

    var csb = new SqlConnectionStringBuilder(connection);
    string database = csb.InitialCatalog;

    var sql = $@"
        BACKUP DATABASE [{database}]
        TO DISK = '{saveFile}'
        WITH FORMAT, COMPRESSION,
             MEDIANAME = '{database}-Data',
             NAME = 'Full Backup of {database}',
             COPY_ONLY;
    ";
    
    using var cn = new SqlConnection(connection);
    using var cmd = new SqlCommand();

    // CommandTimeout is an int measured in seconds
    cmd.CommandTimeout = (int)timeout.TotalSeconds;
    await cn.OpenAsync();
    cmd.CommandText = sql;
    cmd.Connection = cn;

    await cmd.ExecuteNonQueryAsync();
}
```
