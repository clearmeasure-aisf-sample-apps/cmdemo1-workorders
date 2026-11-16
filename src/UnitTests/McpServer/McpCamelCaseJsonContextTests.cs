using System.Text.Json;
using ClearMeasure.Bootcamp.McpServer;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpCamelCaseJsonContextTests
{
    [Test]
    public void Default_Should_WriteIndentedCamelCaseJson()
    {
        McpCamelCaseJsonContext.Default.Options.WriteIndented.ShouldBeTrue();
        McpCamelCaseJsonContext.Default.Options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
    }
}
