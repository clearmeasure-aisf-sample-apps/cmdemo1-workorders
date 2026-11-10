using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class CsvHeaderTests
{
    [Test]
    public void ShouldFindCellsByNameIgnoringCase_WhenTheHeaderNamesThem()
    {
        var header = CsvHeader.Parse(" title ,Description,,TITLE");

        var title = header.Cell(["  Fix boiler ", "Cold", "x", "ignored"], "Title");
        var description = header.Cell(["Fix boiler", "Cold"], "description");

        title.ShouldBe("Fix boiler");
        description.ShouldBe("Cold");
    }

    [TestCase("Room")]
    [TestCase("Description")]
    public void ShouldAnswerNull_WhenTheColumnIsAbsentOrTheCellIsBlankOrMissing(string name)
    {
        var header = CsvHeader.Parse("Title,Description");

        var cell = header.Cell(["Fix boiler", "   "], name);

        cell.ShouldBeNull();
    }

    [Test]
    public void ShouldNameTheFirstMissingColumn_WhenARequiredColumnIsAbsent()
    {
        var header = CsvHeader.Parse("Title,Description");

        var missing = header.FirstMissing(["Title", "CreatorUsername", "Building"]);
        var none = header.FirstMissing(["description"]);

        missing.ShouldBe("CreatorUsername");
        none.ShouldBeNull();
    }
}
