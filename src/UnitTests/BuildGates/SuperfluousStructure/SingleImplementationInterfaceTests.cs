using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class SingleImplementationInterfaceTests
{
    [Test]
    public void ShouldFlagInterface_When_OnlyOneClassImplementsIt()
    {
        const string source = """
            namespace Demo;
            public interface IFoo { void Run(); }
            public sealed class Foo : IFoo { public void Run() { } }
            """;

        StructureFixture.ScanDemo(source).SingleImplementationInterfaces.ShouldBe(["Demo.IFoo"]);
    }

    [Test]
    public void ShouldPassInterface_When_ASecondClassImplementsIt()
    {
        var report = StructureFixture.Scan(repo =>
        {
            repo.Write("src/Core/Core.csproj", StructureFixture.LibraryProject);
            repo.Write("src/Core/IFoo.cs", "namespace Demo; public interface IFoo { void Run(); }");
            repo.Write("src/App/Foo.cs", "using Demo; namespace App; public sealed class Foo : IFoo { public void Run() { } }");
            repo.Write("src/Tests/StubFoo.cs", "using Demo; namespace Tests; public sealed class StubFoo : IFoo { public void Run() { } }");
        });

        report.SingleImplementationInterfaces.ShouldBeEmpty();
    }

    [Test]
    public void ShouldFlagInterface_When_TheOnlyConcreteTypeInheritsTheInterface()
    {
        const string source = """
            namespace Demo;
            public interface IFoo { void Run(); }
            public abstract class BaseFoo : IFoo { public abstract void Run(); }
            public sealed class OnlyFoo : BaseFoo { public override void Run() { } }
            """;

        StructureFixture.ScanDemo(source).SingleImplementationInterfaces.ShouldBe(["Demo.IFoo"]);
    }

    [Test]
    public void ShouldFlagGenericInterface_When_OnlyOneClosedImplementationExists()
    {
        const string source = """
            namespace Demo;
            public interface IHandler<T> { void Handle(T item); }
            public sealed class StringHandler : IHandler<string> { public void Handle(string item) { } }
            """;

        StructureFixture.ScanDemo(source).SingleImplementationInterfaces.ShouldBe(["Demo.IHandler`1"]);
    }

    [Test]
    public void ShouldExemptInterface_When_AllowListGivesAReason()
    {
        const string source = """
            namespace Demo;
            public interface IFoo { void Run(); }
            public sealed class Foo : IFoo { public void Run() { } }
            """;
        var allow = StructureAllowList.Load("""
            { "singleImplementationInterfaces": [
              { "id": "Demo.IFoo", "reason": "Test seam: the fixture substitutes this contract without a second named class." } ] }
            """);

        var report = StructureFixture.Scan(repo =>
        {
            repo.Write("src/Demo/Demo.csproj", StructureFixture.ExeProject);
            repo.Write("src/Demo/Sample.cs", source);
        });
        var problems = StructureRatchet.Evaluate(
            report,
            allow,
            StructureBaseline.Load(StructureFixture.Baseline(StructureRule.SingleFileFolder, 1, "src/Demo")));

        problems.ShouldBeEmpty(string.Join(Environment.NewLine, problems));
        allow.Reason(StructureRule.SingleImplementationInterface, "Demo.IFoo").ShouldContain("Test seam");
    }
}
