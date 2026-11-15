using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class DueDateUrgencyPresenterTests
{
    [Test]
    public void CssClass_WhenNone_IsEmpty()
    {
        DueDateUrgencyPresenter.CssClass(DueDateUrgency.None).ShouldBe(string.Empty);
    }

    [Test]
    public void CssClass_WhenDueToday_IsDueDateToday()
    {
        DueDateUrgencyPresenter.CssClass(DueDateUrgency.DueToday).ShouldBe("due-date-today");
    }

    [Test]
    public void CssClass_WhenOverdue_IsDueDateOverdue()
    {
        DueDateUrgencyPresenter.CssClass(DueDateUrgency.Overdue).ShouldBe("due-date-overdue");
    }

    [Test]
    public void ScreenReaderText_WhenNone_IsNull()
    {
        DueDateUrgencyPresenter.ScreenReaderText(DueDateUrgency.None).ShouldBeNull();
    }

    [Test]
    public void ScreenReaderText_WhenDueToday_IsDueToday()
    {
        DueDateUrgencyPresenter.ScreenReaderText(DueDateUrgency.DueToday).ShouldBe("Due today");
    }

    [Test]
    public void ScreenReaderText_WhenOverdue_IsOverdue()
    {
        DueDateUrgencyPresenter.ScreenReaderText(DueDateUrgency.Overdue).ShouldBe("Overdue");
    }

    [Test]
    public void BadgeText_WhenOverdue_ReturnsOverdue()
    {
        DueDateUrgencyPresenter.BadgeText(DueDateUrgency.Overdue, new DateOnly(2026, 8, 1))
            .ShouldBe("Overdue");
    }

    [Test]
    public void BadgeText_WhenDueToday_ReturnsDueToday()
    {
        DueDateUrgencyPresenter.BadgeText(DueDateUrgency.DueToday, new DateOnly(2026, 8, 29))
            .ShouldBe("Due Today");
    }

    [Test]
    public void BadgeText_WhenNoneWithFutureDate_ReturnsOnTrack()
    {
        DueDateUrgencyPresenter.BadgeText(DueDateUrgency.None, new DateOnly(2026, 9, 30))
            .ShouldBe("On Track");
    }

    [Test]
    public void BadgeText_WhenNoneWithNullDate_ReturnsNull()
    {
        DueDateUrgencyPresenter.BadgeText(DueDateUrgency.None, null)
            .ShouldBeNull();
    }
}
