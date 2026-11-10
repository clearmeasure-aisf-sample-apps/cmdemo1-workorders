using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderSearchSortOptionsTests
{
    [Test]
    public void ShouldOfferNewestFirstThenEachColumnBothWays_WhenTheOptionsAreListed()
    {
        var options = WorkOrderSearchSorter.Options;

        options[0].ShouldBe((string.Empty, "Sort: Newest"));
        options.Count.ShouldBe(1 + SortColumns.All.Count * 2);
        options.ShouldContain(("DueDate:desc", "Sort: Due date ↓"));
    }

    [TestCase(null, false, "")]
    [TestCase(SortColumns.Title, false, "Title:asc")]
    [TestCase(SortColumns.DueDate, true, "DueDate:desc")]
    public void ShouldWriteTheControlValue_WhenASortIsChosen(string? column, bool descending, string expected)
    {
        WorkOrderSearchSorter.OptionValue(column, descending).ShouldBe(expected);
    }

    [Test]
    public void ShouldReadBackTheSort_WhenTheControlValueIsParsed()
    {
        var value = WorkOrderSearchSorter.OptionValue(SortColumns.Room, descending: true);

        WorkOrderSearchSorter.ParseOption(value).ShouldBe((SortColumns.Room, true));
    }
}
