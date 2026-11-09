using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class PassThroughClassTests
{
    [Test]
    public void ShouldFlagClass_When_EveryMemberForwardsToOneField()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Pass.cs", """
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
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.PassThroughClasses.ShouldBe(new[] { "Demo.Pass" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldFlagClass_When_PrimaryConstructorParameterIsTheOnlyTarget()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Pass.cs", """
                namespace Demo;
                public sealed class Inner { public Task Save(string name) => Task.FromResult(name); }
                public sealed class Pass(Inner inner)
                {
                    public async Task Save(string name) => await inner.Save(name);
                }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.PassThroughClasses.ShouldBe(new[] { "Demo.Pass" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassClass_When_AMemberAddsLogic()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Pass.cs", """
                namespace Demo;
                public sealed class Inner { public int Count => 1; }
                public sealed class Pass
                {
                    private readonly Inner _inner;
                    public Pass(Inner inner) { _inner = inner; }
                    public int Count => _inner.Count + 1;
                }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.PassThroughClasses.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassClass_When_AStaticMethodDoesWork()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Pass.cs", """
                namespace Demo;
                public sealed class Inner { public void Save(string name) { _ = name; } }
                public sealed class Pass
                {
                    private readonly Inner _inner;
                    public void Save(string name) => _inner.Save(name);
                    public static string Label() => "pass";
                }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.PassThroughClasses.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassClass_When_ItForwardsToTwoDifferentMembers()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Pass.cs", """
                namespace Demo;
                public sealed class Inner { public void Save(string name) { _ = name; } }
                public sealed class Pass
                {
                    private readonly Inner _left;
                    private readonly Inner _right;
                    public void SaveLeft(string name) => _left.Save(name);
                    public void SaveRight(string name) => _right.Save(name);
                }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.PassThroughClasses.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }
}
