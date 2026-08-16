using MajorSilence.DataAccess;
using Moq;
using NUnit.Framework;

namespace Examples.Tests;

// The "why this makes testing easy" example from the IOC section.  No database:
// TestStuff only ever sees ITestRepo, so a mock is enough.
[TestFixture]
public class TestStuffTests
{
    [Test]
    public void DoStuffInsertsTheName()
    {
        var repo = new Mock<ITestRepo>();
        repo.Setup(x => x.GetName()).Returns("The Name");

        var inst = new MajorSilence.BusinessStuff.TestStuff(repo.Object);
        inst.DoStuff();

        repo.Verify(x => x.InsertData("The Name"), Times.Once);
    }
}
