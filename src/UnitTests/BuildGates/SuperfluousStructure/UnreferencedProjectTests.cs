using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class UnreferencedProjectTests
{
    private const string ReferencesLib = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup><OutputType>Exe</OutputType></PropertyGroup>
          <ItemGroup><ProjectReference Include="..\Lib\Lib.csproj" /></ItemGroup>
        </Project>
        """;

    private const string AnalyzerProps = """
        <Project>
          <PropertyGroup>
            <AnalyzerProject>$(MSBuildThisFileDirectory)Analyzer\Analyzer.csproj</AnalyzerProject>
          </PropertyGroup>
          <ItemGroup><ProjectReference Include="$(AnalyzerProject)" /></ItemGroup>
        </Project>
        """;

    [Test]
    public void ShouldFlagProject_When_NothingReferencesTheLibrary()
    {
        var report = StructureFixture.Scan(repo =>
        {
            StructureFixture.Write(repo, "src/Orphan/Orphan.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Lib/Lib.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/App/App.csproj", ReferencesLib);
        });

        report.UnreferencedProjects.ShouldBe(["src/Orphan/Orphan.csproj"]);
    }

    [Test]
    public void ShouldPassProject_When_HostOrTestProjectIsUnreferenced()
    {
        var report = StructureFixture.Scan(repo =>
        {
            StructureFixture.Write(repo, "src/Database/Database.csproj", StructureFixture.ExeProject);
            StructureFixture.Write(repo, "src/Worker/Worker.csproj", """<Project Sdk="Microsoft.NET.Sdk.Worker"></Project>""");
            StructureFixture.Write(repo, "src/ChurchBulletin.AppHost/ChurchBulletin.AppHost.csproj", """<Project Sdk="Aspire.AppHost.Sdk/13.1.2"></Project>""");
            StructureFixture.Write(repo, "src/UnitTests/UnitTests.csproj", StructureFixture.LibraryProject);
        });

        report.UnreferencedProjects.ShouldBeEmpty();
    }

    [Test]
    public void ShouldPassProject_When_DirectoryBuildPropsReferencesIt()
    {
        var report = StructureFixture.Scan(repo =>
        {
            StructureFixture.Write(repo, "src/Directory.Build.props", AnalyzerProps);
            StructureFixture.Write(repo, "src/Analyzer/Analyzer.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/App/App.csproj", StructureFixture.ExeProject);
        });

        report.UnreferencedProjects.ShouldBeEmpty();
    }
}
