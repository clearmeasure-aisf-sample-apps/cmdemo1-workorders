using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// Configurable <see cref="IWorkOrderBuilder"/> seed for manage-page New-mode renders.
/// </summary>
public sealed class WorkOrderManageNewOrderSource : IWorkOrderBuilder
{
    private Guid? _id;
    private string _number = "WO-NEW";
    private string _title = "New Work Order";
    private string _description = "";
    private WorkOrderStatus _status = WorkOrderStatus.Draft;
    private Employee? _creator;
    private Employee? _assignee;
    private bool _emptyId;

    /// <summary>Sets a fixed work-order id (ignored when <see cref="AsBrandNew"/>).</summary>
    public WorkOrderManageNewOrderSource WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>Uses <see cref="Guid.Empty"/> so the page treats the order as brand-new.</summary>
    public WorkOrderManageNewOrderSource AsBrandNew()
    {
        _emptyId = true;
        return this;
    }

    /// <summary>Sets the work-order number.</summary>
    public WorkOrderManageNewOrderSource WithNumber(string number)
    {
        _number = number;
        return this;
    }

    /// <summary>Sets the title.</summary>
    public WorkOrderManageNewOrderSource WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>Sets the description.</summary>
    public WorkOrderManageNewOrderSource WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the status.</summary>
    public WorkOrderManageNewOrderSource WithStatus(WorkOrderStatus status)
    {
        _status = status;
        return this;
    }

    /// <summary>Overrides the creator stored on the new work order.</summary>
    public WorkOrderManageNewOrderSource WithCreator(Employee creator)
    {
        _creator = creator;
        return this;
    }

    /// <summary>Sets the assignee.</summary>
    public WorkOrderManageNewOrderSource WithAssignee(Employee? assignee)
    {
        _assignee = assignee;
        return this;
    }

    /// <inheritdoc />
    public WorkOrder CreateNewWorkOrder(Employee creator) =>
        WorkOrderBuilder.Create()
            .WithId(_emptyId ? Guid.Empty : _id ?? Guid.NewGuid())
            .WithNumber(_number)
            .WithStatus(_status)
            .CreatedBy(_creator ?? creator)
            .AssignedTo(_assignee)
            .WithTitle(_title)
            .WithDescription(_description)
            .Build();
}
