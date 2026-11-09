using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class FolderShapeTests
{
    [Test]
    public void ShouldFlagFolder_When_ItHoldsOneSourceFile()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/App/App.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/App/Lonely/Only.cs", "namespace Demo; public sealed class Only { }");
            StructureFixture.Write(repo, "src/App/Pair/One.cs", "namespace Demo; public sealed class One { }");
            StructureFixture.Write(repo, "src/App/Pair/Two.cs", "namespace Demo; public sealed class Two { }");

            var report = SuperfluousStructureScanner.Scan(repo);

            report.SingleFileFolders.ShouldBe(new[] { "src/App/Lonely" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassFolder_When_NestingIsFour()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/A/B/C/D/Keep.cs", "namespace Demo; public sealed class Keep { }");
            StructureFixture.Write(repo, "src/A/B/C/D/Also.cs", "namespace Demo; public sealed class Also { }");

            var report = SuperfluousStructureScanner.Scan(repo);

            report.DeepFolders.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldFlagFolder_When_NestingIsDeeperThanFour()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/A/B/C/D/E/Deep.cs", "namespace Demo; public sealed class Deep { }");
            StructureFixture.Write(repo, "src/A/B/C/D/E/Also.cs", "namespace Demo; public sealed class Also { }");

            var report = SuperfluousStructureScanner.Scan(repo);

            report.DeepFolders.ShouldBe(new[] { "src/A/B/C/D/E" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }
}
