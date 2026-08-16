using Microsoft.Data.Sqlite;

namespace Examples.Data;

public static class Program
{
    public static void Main()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"handbook-data-{Guid.NewGuid():N}.db");
        var cnStr = $"Data Source={dbPath}";

        try
        {
            Sqlite.CreateAndQuery(cnStr);

            foreach (var show in Sqlite.GetShows(cnStr))
            {
                Console.WriteLine($"Dapper: {show.ShowName} {show.Rating}");
            }

            Sqlite.Transaction(cnStr);
            Sqlite.LoadDataTable(cnStr);
            EfCore.Run(cnStr);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(dbPath);
        }
    }
}
