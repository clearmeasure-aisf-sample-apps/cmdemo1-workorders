using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;
public class StatusBadgeCssClassMapperTests
{
    [Test]
    [Arguments(nameof(WorkOrderStatus.Draft), "status-badge--draft")]
    [Arguments(nameof(WorkOrderStatus.Assigned), "status-badge--assigned")]
    [Arguments(nameof(WorkOrderStatus.InProgress), "status-badge--in-progress")]
    [Arguments(nameof(WorkOrderStatus.Complete), "status-badge--complete")]
    [Arguments(nameof(WorkOrderStatus.Cancelled), "status-badge--cancelled")]
    public void CssClass_ReturnsCssClassForStatus(string statusKey, string expectedClass)
    {
        var status = WorkOrderStatus.FromKey(statusKey);

        StatusBadgeCssClassMapper.CssClass(status).ShouldBe(expectedClass);
    }

    [Test]
    public void CssClass_WhenNullStatus_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => StatusBadgeCssClassMapper.CssClass(null!));
    }

    [Test]
    public void CssClass_AllStatusesHaveCssClass()
    {
        var allStatuses = WorkOrderStatus.GetAllItems();

        foreach (var status in allStatuses)
        {
            if (!status.IsEmpty())
            {
                var cssClass = StatusBadgeCssClassMapper.CssClass(status);
                cssClass.ShouldNotBeEmpty();
                cssClass.ShouldStartWith("status-badge--");
            }
        }
    }
}

