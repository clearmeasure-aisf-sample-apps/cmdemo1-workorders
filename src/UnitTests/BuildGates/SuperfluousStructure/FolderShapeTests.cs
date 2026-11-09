using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class FolderShapeTests
{
    [Test]
    public void ShouldFlagFolder_When_ItHoldsOneSourceFile()
    {
        var report = StructureFixture.Scan(repo =>
        {
            StructureFixture.Write(repo, "src/App/App.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/App/Lonely/Only.cs", "namespace Demo; public sealed class Only { }");
            StructureFixture.Write(repo, "src/App/Pair/One.cs", "namespace Demo; public sealed class One { }");
            StructureFixture.Write(repo, "src/App/Pair/Two.cs", "namespace Demo; public sealed class Two { }");
        });

        report.SingleFileFolders.ShouldBe(["src/App/Lonely"]);
    }

    [Test]
    public void ShouldPassFolder_When_NestingIsFour()
    {
        var report = StructureFixture.Scan(repo =>
        {
            StructureFixture.Write(repo, "src/A/B/C/D/Keep.cs", "namespace Demo; public sealed class Keep { }");
            StructureFixture.Write(repo, "src/A/B/C/D/Also.cs", "namespace Demo; public sealed class Also { }");
        });

        report.DeepFolders.ShouldBeEmpty();
    }

    [Test]
    public void ShouldFlagFolder_When_NestingIsDeeperThanFour()
    {
        var report = StructureFixture.Scan(repo =>
        {
            StructureFixture.Write(repo, "src/A/B/C/D/E/Deep.cs", "namespace Demo; public sealed class Deep { }");
            StructureFixture.Write(repo, "src/A/B/C/D/E/Also.cs", "namespace Demo; public sealed class Also { }");
        });

        report.DeepFolders.ShouldBe(["src/A/B/C/D/E"]);
    }
}
