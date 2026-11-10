using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools.Arguments;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Tools.Arguments;

[TestFixture]
public class PriorityArgumentTests
{
    [TestCase(null, WorkOrderPriority.Medium)]
    [TestCase("", WorkOrderPriority.Medium)]
    [TestCase("   ", WorkOrderPriority.Medium)]
    [TestCase("Low", WorkOrderPriority.Low)]
    [TestCase("Medium", WorkOrderPriority.Medium)]
    [TestCase("High", WorkOrderPriority.High)]
    [TestCase("low", WorkOrderPriority.Low)]
    [TestCase("HIGH", WorkOrderPriority.High)]
    [TestCase("  medium  ", WorkOrderPriority.Medium)]
    public void ShouldParsePriority_WhenTheTextNamesOneOrIsBlank(string? text, WorkOrderPriority expected)
    {
        var priority = PriorityArgument.Parse(text);

        priority.Value.ShouldBe(expected);
        priority.Error.ShouldBeNull();
    }

    [TestCase("Urgent")]
    [TestCase("2")]
    public void ShouldExplainTheValidValues_WhenTheTextNamesNoPriority(string text)
    {
        var priority = PriorityArgument.Parse(text);

        priority.Error.ShouldBe($"Invalid priority '{text}'. Valid values are: Low, Medium, High.");
    }
}
