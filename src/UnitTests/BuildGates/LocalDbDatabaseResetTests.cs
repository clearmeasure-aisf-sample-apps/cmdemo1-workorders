using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Pins the LocalDB database reset in the build scripts: every build must start
/// from a freshly created database (like the SQL-Container path), and the resolved
/// database name must reach the build, integration and acceptance phases.
/// </summary>
[TestFixture]
public class LocalDbDatabaseResetTests
{
    private const string ResolvedNameAssignment =
        "$script:databaseName = Get-ResolvedDatabaseName -explicitName $databaseName";

    [Test]
    public void ShouldDefineLocalDbReset_WhenBuildFunctionsIsRead()
    {
        string source = RepositoryCheckout.Current.ReadText("BuildFunctions.ps1");
        int start = source.IndexOf("Function Reset-LocalDbDatabase", StringComparison.Ordinal);
        int end = source.IndexOf("Function New-DockerContainerForSqlServer", StringComparison.Ordinal);

        start.ShouldBeGreaterThan(-1);
        end.ShouldBeGreaterThan(start);
        string function = source[start..end];

        function.ShouldContain("[string]$databaseName");
        function.ShouldContain("(LocalDb)\\MSSQLLocalDB");
        function.ShouldContain("IF EXISTS (SELECT name FROM sys.databases WHERE name = N'$databaseName')");
        function.ShouldContain("DROP DATABASE [$databaseName]");
        function.ShouldContain("CREATE DATABASE [$databaseName]");
    }

    [Test]
    public void ShouldResetLocalDbBeforeMigrating_WhenSetupDatabaseForBuildIsRead()
    {
        string source = RepositoryCheckout.Current.ReadText("build.ps1");
        int start = source.IndexOf("Function Setup-DatabaseForBuild", StringComparison.Ordinal);
        int end = source.IndexOf("Function IntegrationTest", StringComparison.Ordinal);

        start.ShouldBeGreaterThan(-1);
        end.ShouldBeGreaterThan(start);
        string function = source[start..end];

        int branch = function.IndexOf("$script:databaseEngine -eq \"LocalDB\"", StringComparison.Ordinal);
        int reset = function.IndexOf("Reset-LocalDbDatabase -databaseName $script:databaseName", StringComparison.Ordinal);
        int migrate = function.IndexOf("MigrateDatabaseLocal", reset, StringComparison.Ordinal);

        branch.ShouldBeGreaterThan(-1);
        reset.ShouldBeGreaterThan(branch);
        migrate.ShouldBeGreaterThan(reset);
    }

    [Test]
    public void ShouldNotClobberTheDatabaseNameParameter_WhenBuildScriptIsRead()
    {
        string source = RepositoryCheckout.Current.ReadText("build.ps1");
        int mainFunctions = source.IndexOf("# \u2500\u2500 Main Functions", StringComparison.Ordinal);

        mainFunctions.ShouldBeGreaterThan(-1);
        string topLevel = source[..mainFunctions];
        topLevel.ShouldNotContain("$databaseName");
    }

    [Test]
    public void ShouldResolveTheDatabaseNameForBuildAndAcceptance_WhenBuildScriptIsRead()
    {
        string source = RepositoryCheckout.Current.ReadText("build.ps1");
        int occurrences = source.Split(ResolvedNameAssignment, StringSplitOptions.None).Length - 1;

        occurrences.ShouldBe(2);
    }

    [Test]
    public void ShouldPassTheDatabaseNameThrough_WhenDriverScriptsAreRead()
    {
        string privateBuild = RepositoryCheckout.Current.ReadText("PrivateBuild.ps1");
        string acceptance = RepositoryCheckout.Current.ReadText("AcceptanceTests.ps1");

        privateBuild.ShouldContain("$buildArgs[\"databaseName\"] = $databaseName");
        privateBuild.ShouldContain("Build @buildArgs");
        acceptance.ShouldContain("$buildArgs[\"databaseName\"] = $databaseName");
        acceptance.ShouldContain("Invoke-AcceptanceTests @buildArgs");
    }
}
