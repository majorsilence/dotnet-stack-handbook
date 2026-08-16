using Dapper;
using Microsoft.Data.Sqlite;

namespace MajorSilence.DataAccess;

public interface ITestRepo
{
    string GetName();
    void InsertData(string name);
}

public class TestRepo : BaseRepo, ITestRepo
{
    public TestRepo(string cnStr) : base(cnStr) { }

    public string GetName()
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

// The variant with no base class, which the chapter prefers.
public class TestRepoNobase : ITestRepo
{
    readonly string cnStr;
    public TestRepoNobase(string cnStr)
    {
        this.cnStr = cnStr;
    }

    public string GetName()
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
