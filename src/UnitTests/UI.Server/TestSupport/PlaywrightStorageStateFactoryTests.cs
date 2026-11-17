using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

[TestFixture]
public class PlaywrightStorageStateFactoryTests
{
    [Test]
    public void ShouldEmbedUserName_WhenForUserCalled()
    {
        var state = PlaywrightStorageStateFactory.ForUser("alice", "https://localhost:7174");
        state.Origins.Single().LocalStorage.Single().Value.ShouldBe("alice");
        state.Origins.Single().LocalStorage.Single().Name.ShouldBe(TestSupportConstants.UserSessionStorageKey);
    }
}
