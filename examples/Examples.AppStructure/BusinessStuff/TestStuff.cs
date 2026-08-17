namespace MajorSilence.BusinessStuff;

// Knows about ITestRepo and nothing else, which is the whole point of the
// exercise: it can be handed a mock in a test with no database in sight.
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
        // GetName returns string?, so the null case has to be handled here
        // rather than discovered later.
        string name = repo.GetName() ?? "(no rows)";

        // Do stuff with the name
        Console.WriteLine($"TestStuff read back: {name}");
    }
}
