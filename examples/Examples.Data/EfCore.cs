using Microsoft.EntityFrameworkCore;

namespace Examples.Data;

// The EF Core listings.  The book's chapters show UseSqlServer; here it is
// UseSqlite so the example can actually run without a server, and because the
// SQLite chapter's version of this listing uses SQLite anyway.
public class EfTvShow
{
    public int Id { get; set; }
    public required string ShowName { get; set; }
    public decimal Rating { get; set; }
}

public class AppDbContext : DbContext
{
    private readonly string _connectionString;

    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<EfTvShow> TvShows { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite(_connectionString);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite has no decimal type, so EF stores it as TEXT and warns that
        // ordering and comparison will not work.  Map it to REAL to match the
        // table the raw ADO.NET example created.
        modelBuilder.Entity<EfTvShow>()
            .Property(x => x.Rating)
            .HasConversion<double>();
    }
}

public static class EfCore
{
    public static void Run(string connectionString)
    {
        using var db = new AppDbContext(connectionString);
        db.Database.EnsureCreated();

        db.TvShows.Add(new EfTvShow { ShowName = "Star Trek", Rating = 4.8m });
        db.SaveChanges();

        var highRated = db.TvShows.Where(t => t.Rating > 4.0m).ToList();
        Console.WriteLine($"EF Core found {highRated.Count} highly rated show(s)");

        // Raw SQL.  FromSql is safe against injection: the interpolated value
        // becomes a SQL parameter, not part of the command text.
        var minRating = 4.0m;
        var shows = db.TvShows
            .FromSql($"SELECT * FROM TvShows WHERE Rating > {minRating}")
            .ToList();
        Console.WriteLine($"FromSql found {shows.Count} show(s)");

        var showName = "Star Trek";
        var result = db.TvShows
            .FromSqlInterpolated($"SELECT * FROM TvShows WHERE ShowName = {showName}")
            .ToList();
        Console.WriteLine($"FromSqlInterpolated found {result.Count} show(s)");

        // Read only queries should skip change tracking.
        var untracked = db.TvShows
            .AsNoTracking()
            .Where(t => t.Rating > 4.0m)
            .ToList();
        Console.WriteLine($"AsNoTracking found {untracked.Count} show(s)");
    }
}
