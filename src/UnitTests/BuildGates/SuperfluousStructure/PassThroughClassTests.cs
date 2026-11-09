using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class PassThroughClassTests
{
    [Test]
    public void ShouldFlagClass_When_EveryMemberForwardsToOneField()
    {
        const string source = """
            namespace Demo;
            public sealed class Inner
            {
                public void Save(string name) { _ = name; }
                public int Count => 1;
            }
            public sealed class Pass
            {
                private readonly Inner _inner;
                public Pass(Inner inner) { _inner = inner; }
                public void Save(string name) => _inner.Save(name);
                public int Count => _inner.Count;
            }
            """;

        StructureFixture.ScanDemo(source).PassThroughClasses.ShouldBe(["Demo.Pass"]);
    }

    [Test]
    public void ShouldFlagClass_When_PrimaryConstructorParameterIsTheOnlyTarget()
    {
        const string source = """
            namespace Demo;
            public sealed class Inner { public System.Threading.Tasks.Task Save(string name) => System.Threading.Tasks.Task.FromResult(name); }
            public sealed class Pass(Inner inner)
            {
                public async System.Threading.Tasks.Task Save(string name) => await inner.Save(name);
            }
            """;

        StructureFixture.ScanDemo(source).PassThroughClasses.ShouldBe(["Demo.Pass"]);
    }

    [Test]
    public void ShouldPassClass_When_AMemberAddsLogic()
    {
        const string source = """
            namespace Demo;
            public sealed class Inner { public int Count => 1; }
            public sealed class Pass
            {
                private readonly Inner _inner;
                public Pass(Inner inner) { _inner = inner; }
                public int Count => _inner.Count + 1;
            }
            """;

        StructureFixture.ScanDemo(source).PassThroughClasses.ShouldBeEmpty();
    }

    [Test]
    public void ShouldPassClass_When_AStaticMethodDoesWork()
    {
        const string source = """
            namespace Demo;
            public sealed class Inner { public void Save(string name) { _ = name; } }
            public sealed class Pass
            {
                private readonly Inner _inner;
                public void Save(string name) => _inner.Save(name);
                public static string Label() => "pass";
            }
            """;

        StructureFixture.ScanDemo(source).PassThroughClasses.ShouldBeEmpty();
    }

    [Test]
    public void ShouldPassClass_When_ItForwardsToTwoDifferentMembers()
    {
        const string source = """
            namespace Demo;
            public sealed class Inner { public void Save(string name) { _ = name; } }
            public sealed class Pass
            {
                private readonly Inner _left;
                private readonly Inner _right;
                public void SaveLeft(string name) => _left.Save(name);
                public void SaveRight(string name) => _right.Save(name);
            }
            """;

        StructureFixture.ScanDemo(source).PassThroughClasses.ShouldBeEmpty();
    }
}
