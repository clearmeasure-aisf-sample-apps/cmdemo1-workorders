using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class SingleSubclassAbstractClassTests
{
    [Test]
    public void ShouldFlagAbstractClass_When_ItHasOneSubclass()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Job.cs", """
                namespace Demo;
                public abstract class Job { public abstract void Run(); }
                public sealed class OnlyJob : Job { public override void Run() { } }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleSubclassAbstractClasses.ShouldBe(new[] { "Demo.Job" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassAbstractClass_When_ItHasTwoSubclasses()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Job.cs", """
                namespace Demo;
                public abstract class Job { public abstract void Run(); }
                public sealed class FirstJob : Job { public override void Run() { } }
                public sealed class SecondJob : Job { public override void Run() { } }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleSubclassAbstractClasses.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassAbstractClass_When_NothingExtendsIt()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/Job.cs", """
                namespace Demo;
                public abstract class Job { public abstract void Run(); }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleSubclassAbstractClasses.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }
}
