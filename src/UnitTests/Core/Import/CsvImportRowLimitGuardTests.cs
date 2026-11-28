using System.Text;
using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class CsvImportRowLimitGuardTests
{
    private const string Header = "Title,Description,CreatorUsername\n";

    [Test]
    public void ShouldUseDefaultOf500_WhenDefaultGuardUsed()
    {
        CsvImportRowLimitGuard.DefaultMaxRows.ShouldBe(500);
        CsvImportRowLimitGuard.Default.MaxRows.ShouldBe(500);
    }

    [Test]
    public void ShouldTreatMaxBelowOneAsOne_WhenConstructedWithZero()
    {
        new CsvImportRowLimitGuard(0).MaxRows.ShouldBe(1);
    }

    [Test]
    public void ShouldAllowRowCount_WhenAtOrBelowMax()
    {
        var guard = new CsvImportRowLimitGuard(3);

        guard.Allows(3).ShouldBeTrue();
        guard.Allows(4).ShouldBeFalse();
    }

    [Test]
    public void ShouldNameTheMax_WhenOverLimitMessageRead()
    {
        new CsvImportRowLimitGuard(42).OverLimitMessage.ShouldContain("42");
    }

    [Test]
    public void ShouldParseAllRows_WhenRowCountEqualsMax()
    {
        var result = Parse(Header + Rows(3), new CsvImportRowLimitGuard(3));

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(3);
    }

    [Test]
    public void ShouldFailNamingMax_WhenRowCountExceedsMax()
    {
        var guard = new CsvImportRowLimitGuard(3);

        var result = Parse(Header + Rows(4), guard);

        result.Success.ShouldBeFalse();
        result.Rows.ShouldBeEmpty();
        result.Error.ShouldBe(guard.OverLimitMessage);
        result.Error!.ShouldContain("3");
    }

    [Test]
    public void ShouldNotCountBlankLines_WhenCheckingLimit()
    {
        var result = Parse(Header + "T1,D1,u1\n\n\nT2,D2,u1\n", new CsvImportRowLimitGuard(2));

        result.Success.ShouldBeTrue();
        result.Rows.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldFailOverDefaultLimit_WhenParsedWithoutExplicitGuard()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(Header + Rows(CsvImportRowLimitGuard.DefaultMaxRows + 1)));

        var result = WorkOrderBulkImportCsvParser.Parse(ms);

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe(CsvImportRowLimitGuard.Default.OverLimitMessage);
    }

    [Test]
    public void ShouldStillReportEmpty_WhenCsvEmptyWithGuard()
    {
        var result = Parse(string.Empty, new CsvImportRowLimitGuard(1));

        result.Success.ShouldBeFalse();
        result.Error.ShouldBe("CSV is empty.");
    }

    [Test]
    public void ShouldReturnNoRows_WhenHeaderOnlyWithGuard()
    {
        var result = Parse(Header, new CsvImportRowLimitGuard(1));

        result.Success.ShouldBeTrue();
        result.Rows.ShouldBeEmpty();
    }

    private static WorkOrderBulkImportParseResult Parse(string csv, CsvImportRowLimitGuard guard)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        return WorkOrderBulkImportCsvParser.Parse(ms, guard);
    }

    private static string Rows(int count) =>
        string.Concat(Enumerable.Range(1, count).Select(i => $"T{i},D{i},u1\n"));
}
