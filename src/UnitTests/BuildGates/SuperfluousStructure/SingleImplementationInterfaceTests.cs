using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class SingleImplementationInterfaceTests
{
    [Test]
    public void ShouldFlagInterface_When_OnlyOneClassImplementsIt()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/IFoo.cs", """
                namespace Demo;
                public interface IFoo { void Run(); }
                public sealed class Foo : IFoo { public void Run() { } }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleImplementationInterfaces.ShouldBe(new[] { "Demo.IFoo" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassInterface_When_ASecondClassImplementsIt()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Core/Core.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Core/IFoo.cs", """
                namespace Demo;
                public interface IFoo { void Run(); }
                """);
            StructureFixture.Write(repo, "src/App/App.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/App/Foo.cs", """
                using Demo;
                namespace App;
                public sealed class Foo : IFoo { public void Run() { } }
                """);
            StructureFixture.Write(repo, "src/Tests/StubFoo.cs", """
                using Demo;
                namespace Tests;
                public sealed class StubFoo : IFoo { public void Run() { } }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleImplementationInterfaces.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldFlagInterface_When_TheOnlyConcreteTypeInheritsTheInterface()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/IFoo.cs", """
                namespace Demo;
                public interface IFoo { void Run(); }
                public abstract class BaseFoo : IFoo { public abstract void Run(); }
                public sealed class OnlyFoo : BaseFoo { public override void Run() { } }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleImplementationInterfaces.ShouldBe(new[] { "Demo.IFoo" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldFlagGenericInterface_When_OnlyOneClosedImplementationExists()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Demo/IHandler.cs", """
                namespace Demo;
                public interface IHandler<T> { void Handle(T item); }
                public sealed class StringHandler : IHandler<string> { public void Handle(string item) { } }
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleImplementationInterfaces.ShouldBe(new[] { "Demo.IHandler`1" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldExemptInterface_When_AllowListGivesAReason()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Demo/Demo.csproj", StructureFixture.ExeProject);
            StructureFixture.Write(repo, "src/Demo/IFoo.cs", """
                namespace Demo;
                public interface IFoo { void Run(); }
                public sealed class Foo : IFoo { public void Run() { } }
                """);
            var report = SuperfluousStructureScanner.Scan(repo);
            var allow = StructureAllowList.Load("""
                {
                  "singleImplementationInterfaces": [
                    { "id": "Demo.IFoo", "reason": "Test seam: the fixture substitutes this contract without a second named class." }
                  ]
                }
                """);

            var problems = StructureRatchet.Evaluate(
                report,
                allow,
                StructureBaseline.Load(StructureFixture.Baseline(StructureRule.SingleFileFolder, 1, "src/Demo")));

            problems.ShouldBeEmpty(string.Join(Environment.NewLine, problems));
            allow.Reason(StructureRule.SingleImplementationInterface, "Demo.IFoo")
                .ShouldContain("Test seam");
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }
}
