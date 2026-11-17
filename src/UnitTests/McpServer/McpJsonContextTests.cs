using System.Text.Json;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpJsonContextTests
{
    [Test]
    public void ToolOptions_Should_ResolveTheToolPayloads_WithoutReflection()
    {
        McpJsonContext.ToolOptions.GetTypeInfo(typeof(EmployeeJson[])).ShouldNotBeNull();
        McpJsonContext.ToolOptions.TypeInfoResolver.ShouldNotBeNull();
    }

    [Test]
    public void ToolOptions_Should_BeOneSharedInstance()
    {
        McpJsonContext.ToolOptions.ShouldBeSameAs(McpJsonContext.ToolOptions);
    }

    [Test]
    public void Default_Should_WriteIndentedPascalCaseJson()
    {
        McpJsonContext.Default.Options.WriteIndented.ShouldBeTrue();
        McpJsonContext.Default.Options.PropertyNamingPolicy.ShouldBeNull();
    }
}
