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
        string name = repo.GetName();

        // Do stuff with the name
        Console.WriteLine($"TestStuff read back: {name}");
    }
}
