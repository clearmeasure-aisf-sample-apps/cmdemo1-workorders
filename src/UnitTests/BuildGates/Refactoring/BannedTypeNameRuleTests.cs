using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

[TestFixture]
public class BannedTypeNameRuleTests
{
    [TestCase("WorkOrderToolsPart1")]
    [TestCase("WorkOrderSearchPart2")]
    [TestCase("ExtraWorkOrderTools")]
    [TestCase("MoreSearchFilters")]
    [TestCase("MiscWorkOrderHandlers")]
    [TestCase("Manager2")]
    public void ShouldFlagTypeName_WhenItIsNumberedOrVague(string name)
    {
        var sources = new SourceSetBuilder().WithFile("src/Core/Sample.cs", $"public class {name} {{ }}").Build();

        var violations = SourceSetBuilder.KeysOf(new BannedTypeNameRule(), sources);

        violations.ShouldBe([$"src/Core/Sample.cs :: {name}"]);
    }

    [TestCase("WorkOrderAssignment")]
    [TestCase("ToolsSha256")]
    [TestCase("Base64Codec")]
    [TestCase("Http2ChannelFactory")]
    public void ShouldPassTypeName_WhenItNamesAConceptOrATechnicalNumber(string name)
    {
        var sources = new SourceSetBuilder().WithFile("src/Core/Sample.cs", $"public class {name} {{ }}").Build();

        var violations = SourceSetBuilder.KeysOf(new BannedTypeNameRule(), sources);

        violations.ShouldBeEmpty();
    }
}
