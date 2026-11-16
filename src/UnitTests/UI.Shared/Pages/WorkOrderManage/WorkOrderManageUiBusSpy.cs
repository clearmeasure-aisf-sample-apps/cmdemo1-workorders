using ClearMeasure.Bootcamp.UI.Shared;
using Palermo.BlazorMvc;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// Counts <see cref="WorkOrderSelectedEvent"/> notifications for manage-page event-bus tests.
/// </summary>
public sealed class WorkOrderManageUiBusSpy : IUiBus
{
    /// <summary>How many times a work-order selection was published.</summary>
    public int NotifiedWorkOrderSelectedCount { get; private set; }

    /// <summary>How many times the non-generic <see cref="Notify(object)"/> overload was used.</summary>
    public int NonGenericNotifyCount { get; private set; }

    /// <summary>Non-generic notify used to characterize object-overload counting.</summary>
    public void Notify(object eventObject)
    {
        NonGenericNotifyCount++;
        if (eventObject is WorkOrderSelectedEvent)
        {
            NotifiedWorkOrderSelectedCount++;
        }
    }

    /// <inheritdoc />
    public void Register(IListener listener)
    {
    }

    /// <inheritdoc />
    public void UnRegister(IListener listener)
    {
    }

    /// <inheritdoc />
    public IListener<T>[] GetListeners<T>() where T : IUiBusEvent => [];

    /// <inheritdoc />
    public void Notify<T>(T eventObject) where T : IUiBusEvent
    {
        if (eventObject is WorkOrderSelectedEvent)
        {
            NotifiedWorkOrderSelectedCount++;
        }
    }

    /// <inheritdoc />
    public void UnRegisterAll()
    {
    }
}
