using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Database;

/// <summary>
/// Static checks on <c>035_AddTeamAndVendorTables.sql</c> so the SQLite build also covers the migration:
/// it is the newest script, only adds tables, uses tabs, and sizes columns from the entity constants.
/// </summary>
[TestFixture]
public class AddTeamAndVendorTablesScriptTests
{
    private const string ScriptName = "035_AddTeamAndVendorTables.sql";

    private static string UpdateDirectory =>
        Path.Combine(FindRepoRoot(), "src", "Database", "scripts", "Update");

    private static string ReadScript() => File.ReadAllText(Path.Combine(UpdateDirectory, ScriptName));

    [Test]
    public void ShouldBeTheOnly035ScriptAndTheHighestNumber()
    {
        var scripts = Directory.GetFiles(UpdateDirectory, "*.sql").Select(Path.GetFileName).ToArray();

        scripts.Where(name => name!.StartsWith("035", StringComparison.Ordinal)).ShouldBe([ScriptName]);
        scripts.Max(StringComparer.Ordinal).ShouldBe(ScriptName);
    }

    [Test]
    public void ShouldIndentWithTabsOnly()
    {
        var lines = File.ReadAllLines(Path.Combine(UpdateDirectory, ScriptName));

        lines.Where(line => line.StartsWith(' ')).ShouldBeEmpty();
    }

    [Test]
    public void ShouldCreateTheThreeNewTables()
    {
        var script = ReadScript();

        script.ShouldContain("CREATE TABLE [dbo].[Team]");
        script.ShouldContain("CREATE TABLE [dbo].[Vendor]");
        script.ShouldContain("CREATE TABLE [dbo].[TeamMember]");
    }

    [Test]
    public void ShouldNotAlterDropOrRenameExistingObjects()
    {
        var script = ReadScript();

        script.ShouldNotContain("ALTER TABLE", Case.Insensitive);
        script.ShouldNotContain("DROP ", Case.Insensitive);
        script.ShouldNotContain("sp_rename", Case.Insensitive);
    }

    [TestCase("Name", Team.NameMaxLength, "Team")]
    [TestCase("Description", Team.DescriptionMaxLength, "Team")]
    [TestCase("Name", Vendor.NameMaxLength, "Vendor")]
    [TestCase("Trade", Vendor.TradeMaxLength, "Vendor")]
    [TestCase("ContactName", Vendor.ContactNameMaxLength, "Vendor")]
    [TestCase("EmailAddress", Vendor.EmailAddressMaxLength, "Vendor")]
    [TestCase("PhoneNumber", Vendor.PhoneNumberMaxLength, "Vendor")]
    public void ShouldSizeColumnFromEntityConstant(string column, int maxLength, string table)
    {
        var tableBody = Regex.Match(ReadScript(),
            @"CREATE TABLE \[dbo\]\.\[" + table + @"\]\s*\((?<body>.*?)\n\t\)", RegexOptions.Singleline);
        tableBody.Success.ShouldBeTrue();

        tableBody.Groups["body"].Value.ShouldMatch(@"\[" + column + @"\]\s+NVARCHAR\(" + maxLength + @"\)");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "ChurchBulletin.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test directory.");
    }
}
