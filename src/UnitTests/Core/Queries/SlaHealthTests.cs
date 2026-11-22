using System.Reflection;
using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class SlaHealthTests
{
    [Test]
    public void SimulateRemoteObject_EmptySlaHealth_ShouldRoundTripDefaults()
    {
        var rehydrated = RemotableRequestTests.SimulateRemoteObject(new SlaHealth());

        rehydrated.CurrentMonthPercentage.ShouldBeNull();
        rehydrated.LastMonthPercentage.ShouldBeNull();
        rehydrated.CurrentMonthOrderCount.ShouldBe(0);
        rehydrated.LastMonthOrderCount.ShouldBe(0);
    }

    [Test]
    public void SimulateRemoteObject_PopulatedSlaHealth_ShouldRoundTripEveryProperty()
    {
        var health = new SlaHealth
        {
            CurrentMonthPercentage = 62m,
            LastMonthPercentage = 88m,
            CurrentMonthOrderCount = 8,
            LastMonthOrderCount = 16
        };

        var rehydrated = RemotableRequestTests.SimulateRemoteObject(health);

        rehydrated.CurrentMonthPercentage.ShouldBe(62m);
        rehydrated.LastMonthPercentage.ShouldBe(88m);
        rehydrated.CurrentMonthOrderCount.ShouldBe(8);
        rehydrated.LastMonthOrderCount.ShouldBe(16);
    }

    [Test]
    public void SimulateRemoteObject_SlaHealthQuery_ShouldRoundTrip()
    {
        var rehydrated = RemotableRequestTests.SimulateRemoteObject(new SlaHealthQuery());

        rehydrated.ShouldBeOfType<SlaHealthQuery>();
        rehydrated.ShouldBe(new SlaHealthQuery());
    }

    [Test]
    public void SlaHealth_ShouldDeclareNoPublicStaticMembers()
    {
        var staticMembers = typeof(SlaHealth).GetMembers(BindingFlags.Public | BindingFlags.Static);

        staticMembers.ShouldBeEmpty();
    }
}
