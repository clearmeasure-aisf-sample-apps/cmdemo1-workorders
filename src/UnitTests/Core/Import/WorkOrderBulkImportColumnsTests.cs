using ClearMeasure.Bootcamp.Core.Import;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Import;

[TestFixture]
public class WorkOrderBulkImportColumnsTests
{
    private static readonly string[] AllColumns =
    [
        WorkOrderBulkImportColumns.Title,
        WorkOrderBulkImportColumns.Description,
        WorkOrderBulkImportColumns.CreatorUsername,
        WorkOrderBulkImportColumns.RoomNumber,
        WorkOrderBulkImportColumns.Building,
        WorkOrderBulkImportColumns.Priority,
        WorkOrderBulkImportColumns.DueDate
    ];

    [Test]
    public void ExpectedHeader_WhenBuilt_ShouldListEveryColumnInOrder()
    {
        WorkOrderBulkImportColumns.ExpectedHeader.ShouldBe(
            "Title, Description, CreatorUsername, RoomNumber (optional), Building (optional), "
            + "Priority (optional), DueDate (optional)");
    }

    [TestCaseSource(nameof(AllColumns))]
    public void ExpectedHeader_WhenBuilt_ShouldContainColumnConstant(string column)
    {
        WorkOrderBulkImportColumns.ExpectedHeader.ShouldContain(column);
    }

    [Test]
    public void RequiredAndOptional_WhenCombined_ShouldCoverEveryColumnExactlyOnce()
    {
        WorkOrderBulkImportColumns.Required.ShouldBe(["Title", "Description", "CreatorUsername"]);
        WorkOrderBulkImportColumns.Optional.ShouldBe(["RoomNumber", "Building", "Priority", "DueDate"]);
        WorkOrderBulkImportColumns.Required.Concat(WorkOrderBulkImportColumns.Optional).ShouldBe(AllColumns);
    }
}
