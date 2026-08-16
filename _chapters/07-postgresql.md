---
layout: chapter
title: "PostgreSQL"
number: 7
part: 3
---

## PostgreSQL - Install

Follow the instructions found at [https://www.postgresql.org/download/](https://www.postgresql.org/download/).   

See the majorsilence [PostgreSQL](/posts/2024/06/04/postgresql.html) page for fedora and ubuntu configuration instructions instructions.

For managing PostgreSQL databases use [pgAdmin](https://www.pgadmin.org/).

## PostgreSQL Examples

### Create a Table

```sql
CREATE TABLE tv_shows (
    id SERIAL PRIMARY KEY,
    show_name VARCHAR(100) NOT NULL,
    rating NUMERIC(3,1)
);
```

### Insert Data

```sql
INSERT INTO tv_shows (show_name, rating) VALUES ('Friends', 4.8);
INSERT INTO tv_shows (show_name, rating) VALUES ('Dexter', 4.5);
```

### Stored Procedure

A stored procedure to insert a new TV show:

```sql
CREATE OR REPLACE PROCEDURE insert_tv_show(p_show_name VARCHAR, p_rating NUMERIC)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO tv_shows (show_name, rating) VALUES (p_show_name, p_rating);
END;
$$;
```

Call the procedure:

```sql
CALL insert_tv_show('Frasier', 4.6);
```

### Stored Function

A function to get the average rating:

```sql
CREATE OR REPLACE FUNCTION get_average_rating()
RETURNS NUMERIC AS $$
BEGIN
    RETURN (SELECT AVG(rating) FROM tv_shows);
END;
$$ LANGUAGE plpgsql;
```

Usage:

```sql
SELECT get_average_rating();
```

### View

A view showing only highly rated shows:

```sql
CREATE OR REPLACE VIEW high_rated_shows AS
SELECT id, show_name, rating
FROM tv_shows
WHERE rating >= 4.5;
```

Query the view:

```sql
SELECT * FROM high_rated_shows;
```

### C# Example: Querying PostgreSQL

Install the [Npgsql](https://www.npgsql.org/) NuGet package:

```bash
dotnet add package Npgsql
```

Sample C# code:

```cs
using Npgsql;

var connString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb";
using var conn = new NpgsqlConnection(connString);
conn.Open();

// Query data
using var cmd = new NpgsqlCommand("SELECT id, show_name, rating FROM tv_shows", conn);
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"{reader.GetInt32(0)}: {reader.GetString(1)} ({reader.GetDecimal(2)})");
}

// Call a function
using var avgCmd = new NpgsqlCommand("SELECT get_average_rating()", conn);
var avg = avgCmd.ExecuteScalar();
Console.WriteLine($"Average rating: {avg}");
```

**Note:** For async usage, use `await conn.OpenAsync()` and `await cmd.ExecuteReaderAsync()`.

## PostgreSQL with Dapper

[Dapper](https://github.com/DapperLib/Dapper) is a lightweight ORM for .NET that works well with PostgreSQL via the [Npgsql](https://www.npgsql.org/) driver.

**Install NuGet packages:**

```bash
dotnet add package Dapper
dotnet add package Npgsql
```

**Example: Querying PostgreSQL with Dapper**

```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

public class TvShow
{
    public int Id { get; set; }
    public string ShowName { get; set; }
    public decimal Rating { get; set; }
}

public class Example
{
    public async Task<IEnumerable<TvShow>> GetShowsAsync()
    {
        var connString = "Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb";
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        var sql = "SELECT id, show_name AS ShowName, rating FROM tv_shows WHERE rating > @minRating";
        return await conn.QueryAsync<TvShow>(sql, new { minRating = 4.0m });
    }
}
```

## PostgreSQL with Entity Framework Core

**Install NuGet packages:**

```bash
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

**Example: DbContext and Model**

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
        => options.UseNpgsql("Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb");
}

// Usage
using var db = new AppDbContext();
db.TvShows.Add(new TvShow { ShowName = "Friends", Rating = 4.8m });
db.SaveChanges();

var highRated = db.TvShows.Where(t => t.Rating > 4.0m).ToList();
```

**Note:**  
- Use migrations to create/update your PostgreSQL schema:  
  `dotnet ef migrations add InitialCreate`  
  `dotnet ef database update`
- See [Npgsql EF Core docs](https://www.npgsql.org/efcore/) for advanced usage.
