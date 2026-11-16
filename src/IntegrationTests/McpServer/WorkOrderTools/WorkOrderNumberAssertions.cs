using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer.WorkOrderTools;

/// <summary>Set equality for work-order number collections in MCP list-filter tests.</summary>
internal static class WorkOrderNumberAssertions
{
    public static void ShouldBeSet(this IEnumerable<string> actual, params string[] expected)
    {
        actual.OrderBy(number => number).ShouldBe(expected.OrderBy(number => number));
    }
}
