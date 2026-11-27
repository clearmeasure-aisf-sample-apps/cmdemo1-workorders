using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

[TestFixture]
public class SaveDraftOutcomeTests
{
    private const string Number = "AB12CD3";
    private const string ManageUrl = "https://localhost:7174/workorder/manage?mode=New";

    [Test]
    public void ShouldBlameTheApp_WhenTheErrorBarIsShown()
    {
        var state = new SaveDraftPageState(ManageUrl, true, ["Title is required."], Number);

        var message = SaveDraftOutcome.NotNavigatedMessage(state, Number, 90_000);

        message.ShouldBe(
            "Save Draft of work order AB12CD3 did not reach /workorder/search within 90000 ms; the page stayed on " + ManageUrl + ". " +
            "The app threw while saving (Blazor's error bar is shown): look for the exception in the server log.");
    }

    [Test]
    public void ShouldListTheValidationMessages_WhenTheFormRefusedTheValues()
    {
        var state = new SaveDraftPageState(ManageUrl, false, ["The Title field is required.", "Room number is too long."], Number);

        var message = SaveDraftOutcome.NotNavigatedMessage(state, Number, 90_000);

        message.ShouldEndWith("The form refused the values: The Title field is required. | Room number is too long.");
    }

    [Test]
    public void ShouldReportTheReload_WhenTheFormShowsAnotherNumber()
    {
        var state = new SaveDraftPageState(ManageUrl, false, [], "ZZ99ZZ9");

        var message = SaveDraftOutcome.NotNavigatedMessage(state, Number, 90_000);

        message.ShouldEndWith(
            "The form now shows work order ZZ99ZZ9: it was loaded again after the test read the number, which clears what the test typed.");
    }

    [Test]
    public void ShouldSayNone_WhenTheFormShowsNoNumber()
    {
        var state = new SaveDraftPageState(ManageUrl, false, [], null);

        SaveDraftOutcome.NotNavigatedMessage(state, Number, 90_000).ShouldContain("shows work order (none)");
    }

    [Test]
    public void ShouldReportALostClick_WhenThePageShowsNothingWrong()
    {
        var state = new SaveDraftPageState(ManageUrl, false, [], Number);

        var message = SaveDraftOutcome.NotNavigatedMessage(state, Number, 90_000);

        message.ShouldEndWith("No error and no validation message: the click did not submit the form.");
    }

    [Test]
    public void ShouldFormatTheTimeoutWithTheInvariantCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("fi-FI");
            var state = new SaveDraftPageState(ManageUrl, false, [], Number);

            SaveDraftOutcome.NotNavigatedMessage(state, Number, 1234f).ShouldContain("within 1234 ms");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
