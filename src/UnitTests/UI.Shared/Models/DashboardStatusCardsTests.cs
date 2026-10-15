using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class DashboardStatusCardsTests
{
    [Test]
    public void Build_ReturnsFiveCardsInStatusOrder()
    {
        var counts = new Dictionary<string, int>
        {
            ["Draft"] = 1, ["Assigned"] = 2, ["InProgress"] = 3, ["Complete"] = 4, ["Cancelled"] = 5
        };

        var cards = DashboardStatusCards.Build(counts);

        cards.Select(c => c.Status).ShouldBe(WorkOrderStatus.GetAllItems());
        cards.Select(c => c.Count).ShouldBe([1, 2, 3, 4, 5]);
    }

    [Test]
    public void Build_MissingKeyOrNullCounts_IsZero()
    {
        var counts = new Dictionary<string, int> { ["Assigned"] = 7 };

        var partial = DashboardStatusCards.Build(counts);
        var none = DashboardStatusCards.Build(null);

        partial.Select(c => c.Count).ShouldBe([0, 7, 0, 0, 0]);
        none.Length.ShouldBe(5);
        none.ShouldAllBe(c => c.Count == 0);
    }

    [TestCase("Draft", "summary-card--status-draft", "bi-pencil-square")]
    [TestCase("Assigned", "summary-card--status-assigned", "bi-person-check")]
    [TestCase("InProgress", "summary-card--status-in-progress", "bi-hourglass-split")]
    [TestCase("Complete", "summary-card--status-complete", "bi-check2-circle")]
    [TestCase("Cancelled", "summary-card--status-cancelled", "bi-x-circle")]
    public void Build_AssignsToneAndIconPerStatus(string key, string tone, string icon)
    {
        var cards = DashboardStatusCards.Build(new Dictionary<string, int>());

        var card = cards.Single(c => c.Status.Key == key);

        card.Tone.ShouldBe(tone);
        card.Icon.ShouldBe(icon);
    }
}
