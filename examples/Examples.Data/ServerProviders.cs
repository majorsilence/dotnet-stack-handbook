using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Examples.Data;

// The SQL Server and PostgreSQL listings.  Compiled but never called: they need a
// live server, and the point of this tree is that the book's code builds.  If a
// method signature or a package API changes out from under the book, the build
// still catches it here.
public static class ServerProviders
{
    public static void SqlServerTransaction(string connectionString)
    {
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
    }

    public static void PostgresTransaction(string connectionString)
    {
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
    }
}
