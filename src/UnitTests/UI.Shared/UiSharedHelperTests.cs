// ReSharper disable UnusedAutoPropertyAccessor.Local -- test message properties are set so activity tagging can read them
using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class BusActivityTaggerTests
{
    [Test]
    public void ShouldTagScalarProperties_WhenMessageHasValues()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllData;
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("Test");
        using var activity = source.CreateActivity("bus", ActivityKind.Internal)?.Start();
        activity.ShouldNotBeNull();

        BusActivityTagger.AddScalarPropertyTags(new TestBusMessage { Number = "WO-1" }, activity);

        activity.GetTagItem("bus.message.Number").ShouldBe("WO-1");
    }

    [Test]
    public void ShouldExcludeMarkedFreeFormProperties()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllData;
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("Test");
        using var activity = source.CreateActivity("bus", ActivityKind.Internal)?.Start();
        activity.ShouldNotBeNull();
        var command = new CreateDatedWorkOrdersCommand(
            "tlovejoy",
            "gwillie",
            "Sensitive title",
            "Sensitive description",
            [new DateOnly(2026, 8, 29)]);

        BusActivityTagger.AddScalarPropertyTags(command, activity);
        BusActivityTagger.AddScalarPropertyTags(
            new ApplicationChatQuery("Sensitive chat prompt", "tlovejoy"),
            activity);

        activity.GetTagItem("bus.message.CreatorUsername").ShouldBe("tlovejoy");
        activity.GetTagItem("bus.message.AssigneeUsername").ShouldBe("gwillie");
        activity.GetTagItem("bus.message.Title").ShouldBeNull();
        activity.GetTagItem("bus.message.Description").ShouldBeNull();
        activity.GetTagItem("bus.message.Prompt").ShouldBeNull();
        activity.GetTagItem("bus.message.CurrentUsername").ShouldBe("tlovejoy");
    }
    [Test]
    public void ShouldSkipPropertyTags_WhenObjectIsNotABusMessage()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllData;
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("Test");
        using var activity = source.CreateActivity("bus", ActivityKind.Internal)?.Start();
        activity.ShouldNotBeNull();

        BusActivityTagger.AddScalarPropertyTags(new UnmarkedMessage { Number = "WO-2" }, activity);

        activity.GetTagItem("bus.message.Number").ShouldBeNull();
    }


    private static ActivitySamplingResult SampleAllData(ref ActivityCreationOptions<ActivityContext> _) =>
        ActivitySamplingResult.AllData;

    private sealed class TestBusMessage : IBusMessage
    {
        public string Number { get; init; } = string.Empty;
    }
    private sealed class UnmarkedMessage
    {
        public string Number { get; init; } = string.Empty;
    }
}
