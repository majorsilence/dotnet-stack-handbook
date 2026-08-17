using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Examples.Data;

// The listings from the SQLite chapter and the SQLite half of "Data Access in
// .NET".  Everything here runs for real against a temporary database file.
public static class Sqlite
{
    public static void CreateAndQuery(string connectionString)
    {
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
    }

    // --- Dapper ------------------------------------------------------------------
    public static IEnumerable<TvShow> GetShows(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();
        return conn.Query<TvShow>("SELECT Id, ShowName, Rating FROM TvShows WHERE Rating > @minRating", new { minRating = 4.0 });
    }

    // --- DbTransaction -----------------------------------------------------------
    public static void Transaction(string connectionString)
    {
        using DbConnection conn = new SqliteConnection(connectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = "UPDATE TvShows SET Rating = Rating + 0.1 WHERE ShowName = $name";
            // DbCommand.Parameters is a DbParameterCollection, which has no
            // AddWithValue.  CreateParameter is the provider neutral way.
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = "Friends";
            cmd.Parameters.Add(p);
            cmd.ExecuteNonQuery();

            transaction.Commit();
            Console.WriteLine("Transaction committed.");
        }
        catch
        {
            transaction.Rollback();
            Console.WriteLine("Transaction rolled back.");
        }
    }

    // --- DataTable from a reader -------------------------------------------------
    public static void LoadDataTable(string connectionString)
    {
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM TvShows";

        var table = new DataTable();
        using var reader = cmd.ExecuteReader();
        table.Load(reader);

        Console.WriteLine($"DataTable loaded {table.Rows.Count} row(s), {table.Columns.Count} columns");
    }
}

public class TvShow
{
    public int Id { get; set; }
    public required string ShowName { get; set; }
    public double Rating { get; set; }
}
