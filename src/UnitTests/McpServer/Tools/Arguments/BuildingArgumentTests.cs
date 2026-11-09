using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class BuildingArgumentTests
{
    [TestCase(null, null)]
    [TestCase("", null)]
    [TestCase("   ", null)]
    [TestCase("Main Hall", "Main Hall")]
    [TestCase("  East Wing  ", "East Wing")]
    public void ShouldTrimTheBuilding_WhenItFitsOrIsBlank(string? text, string? expected)
    {
        var building = BuildingArgument.Parse(text);

        building.Value.ShouldBe(expected);
        building.Error.ShouldBeNull();
    }

    [Test]
    public void ShouldAcceptTheBuilding_WhenItHasTheMaximumLength()
    {
        var name = new string('x', WorkOrder.BuildingMaxLength);

        var building = BuildingArgument.Parse(name);

        building.Value.ShouldBe(name);
        building.Error.ShouldBeNull();
    }

    [Test]
    public void ShouldRejectTheBuilding_WhenItIsLongerThanTheMaximum()
    {
        var name = new string('x', WorkOrder.BuildingMaxLength + 1);

        var building = BuildingArgument.Parse(name);

        building.Value.ShouldBeNull();
        building.Error.ShouldBe("Building cannot exceed 100 characters.");
    }
}
