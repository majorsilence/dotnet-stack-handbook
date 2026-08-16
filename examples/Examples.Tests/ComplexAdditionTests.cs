using NUnit.Framework;

namespace Examples.Tests;

// The NUnit example from "Structuring an Application".
[TestFixture]
public class ComplexAdditionTests
{
    [Test]
    public async Task CalculationsCalculatesTest()
    {
        var complexAdds = new ComplexAddition();

        // 10 outer iterations, each adding 1 once per inner value 0..999
        const int expectedResult = 10 * 1000;
        int actualResult = await complexAdds.CalculateWithLock(10, 999);

        Assert.That(actualResult, Is.EqualTo(expectedResult));
    }
}

public class ComplexAddition
{
    public async Task<int> CalculateWithLock(int outerLimit = 10, int innerLimit = 999)
    {
        var tasks = new List<Task>();
        var lockObject = new object();

        int count = 0;
        for (int i = 0; i < outerLimit; i++)
        {
            tasks.Add(Task.Factory.StartNew(() =>
            {
                for (int j = 0; j <= innerLimit; j++)
                {
                    lock (lockObject)
                    {
                        count = count + 1;
                    }
                }
            }));
        }

        foreach (var t in tasks)
        {
            await t;
        }

        return count;
    }
}
