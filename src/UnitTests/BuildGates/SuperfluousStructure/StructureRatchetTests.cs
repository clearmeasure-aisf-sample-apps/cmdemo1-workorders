using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class StructureRatchetTests
{
    [Test]
    public void ShouldFail_When_ReasonIsMissing()
    {
        var json = """{ "singleImplementationInterfaces": [ { "id": "Demo.IFoo" } ] }""";

        Should.Throw<InvalidDataException>(() => StructureAllowList.Load(json));
    }

    [Test]
    public void ShouldFail_When_ReasonIsBlank()
    {
        var json = """{ "singleImplementationInterfaces": [ { "id": "Demo.IFoo", "reason": "  " } ] }""";

        Should.Throw<InvalidDataException>(() => StructureAllowList.Load(json));
    }

    [Test]
    public void ShouldFail_When_NewIdAppears()
    {
        var report = Report(["Demo.IFoo", "Demo.IBar"]);
        var baseline = StructureBaseline.Load(
            StructureFixture.Baseline(StructureRule.SingleImplementationInterface, 1, "Demo.IFoo"));

        var problems = StructureRatchet.Evaluate(report, EmptyAllow(), baseline);

        problems.ShouldContain("singleImplementationInterfaces: new id Demo.IBar");
    }

    [Test]
    public void ShouldFail_When_CountExceedsMax()
    {
        var report = Report(["Demo.IFoo", "Demo.IBar"]);
        var baseline = StructureBaseline.Load(
            StructureFixture.Baseline(StructureRule.SingleImplementationInterface, 1, "Demo.IFoo"));

        var problems = StructureRatchet.Evaluate(report, EmptyAllow(), baseline);

        problems.ShouldContain("singleImplementationInterfaces: count 2 exceeds max 1");
    }

    [Test]
    public void ShouldFail_When_MaxIsBelowTheGrandfatheredList()
    {
        Should.Throw<InvalidDataException>(() => StructureBaseline.Load(
            StructureFixture.Baseline(StructureRule.SingleImplementationInterface, 0, "Demo.IFoo")));
    }

    [Test]
    public void ShouldPass_When_CountDrops()
    {
        var report = Report(["Demo.IFoo"]);
        var baseline = StructureBaseline.Load(
            StructureFixture.Baseline(StructureRule.SingleImplementationInterface, 2, "Demo.IFoo", "Demo.IBar"));

        var problems = StructureRatchet.Evaluate(report, EmptyAllow(), baseline);

        problems.ShouldBeEmpty();
    }

    private static StructureReport Report(string[] interfaces)
    {
        return new StructureReport { SingleImplementationInterfaces = interfaces };
    }

    private static StructureAllowList EmptyAllow()
    {
        return StructureAllowList.Load(StructureFixture.EmptyAllowList());
    }
}
