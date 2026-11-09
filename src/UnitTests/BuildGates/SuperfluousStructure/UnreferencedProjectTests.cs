using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

[TestFixture]
public class UnreferencedProjectTests
{
    [Test]
    public void ShouldFlagProject_When_NothingReferencesTheLibrary()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Orphan/Orphan.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/Lib/Lib.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/App/App.csproj", """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\Lib\Lib.csproj" />
                  </ItemGroup>
                </Project>
                """);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.UnreferencedProjects.ShouldBe(new[] { "src/Orphan/Orphan.csproj" });
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassProject_When_HostOrTestProjectIsUnreferenced()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Database/Database.csproj", StructureFixture.ExeProject);
            StructureFixture.Write(repo, "src/Worker/Worker.csproj", """
                <Project Sdk="Microsoft.NET.Sdk.Worker"></Project>
                """);
            StructureFixture.Write(repo, "src/ChurchBulletin.AppHost/ChurchBulletin.AppHost.csproj", """
                <Project Sdk="Aspire.AppHost.Sdk/13.1.2"></Project>
                """);
            StructureFixture.Write(repo, "src/UnitTests/UnitTests.csproj", StructureFixture.LibraryProject);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.UnreferencedProjects.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }

    [Test]
    public void ShouldPassProject_When_DirectoryBuildPropsReferencesIt()
    {
        var repo = StructureFixture.CreateRepo();
        try
        {
            StructureFixture.Write(repo, "src/Directory.Build.props", """
                <Project>
                  <PropertyGroup>
                    <AnalyzerProject>$(MSBuildThisFileDirectory)Analyzer\Analyzer.csproj</AnalyzerProject>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="$(AnalyzerProject)" />
                  </ItemGroup>
                </Project>
                """);
            StructureFixture.Write(repo, "src/Analyzer/Analyzer.csproj", StructureFixture.LibraryProject);
            StructureFixture.Write(repo, "src/App/App.csproj", StructureFixture.ExeProject);

            var report = SuperfluousStructureScanner.Scan(repo);

            report.UnreferencedProjects.ShouldBeEmpty();
        }
        finally
        {
            StructureFixture.Delete(repo);
        }
    }
}
