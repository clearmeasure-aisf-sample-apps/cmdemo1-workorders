using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class SingleSubclassAbstractClassTests
{
    [Test]
    public void ShouldFlagAbstractClass_When_ItHasOneSubclass()
    {
        const string source = """
            namespace Demo;
            public abstract class Job { public abstract void Run(); }
            public sealed class OnlyJob : Job { public override void Run() { } }
            """;

        StructureFixture.ScanDemo(source).SingleSubclassAbstractClasses.ShouldBe(["Demo.Job"]);
    }

    [Test]
    public void ShouldPassAbstractClass_When_ItHasTwoSubclasses()
    {
        const string source = """
            namespace Demo;
            public abstract class Job { public abstract void Run(); }
            public sealed class FirstJob : Job { public override void Run() { } }
            public sealed class SecondJob : Job { public override void Run() { } }
            """;

        StructureFixture.ScanDemo(source).SingleSubclassAbstractClasses.ShouldBeEmpty();
    }

    [Test]
    public void ShouldPassAbstractClass_When_NothingExtendsIt()
    {
        const string source = """
            namespace Demo;
            public abstract class Job { public abstract void Run(); }
            """;

        StructureFixture.ScanDemo(source).SingleSubclassAbstractClasses.ShouldBeEmpty();
    }
}
