using Bunit;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class DictationTests
{
    [Test]
    public async Task ShouldRenderDictateTitleButton()
    {
        var page = NewDictationPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var element = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle));
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public async Task ShouldRenderDictateDescriptionButton()
    {
        var page = NewDictationPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var element = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateDescription));
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
            element.GetAttribute("aria-pressed").ShouldBe("false");
        });
    }

    [Test]
    public async Task DictateTitleButtonShouldShowListeningStateWhenClicked()
    {
        var page = NewDictationPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle)).ClickAsync(new());
        await AssertPressed(component, true, "btn-app--danger");

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle)).ClickAsync(new());
        await AssertPressed(component, false, "btn-app--secondary");
    }

    [Test]
    public async Task ShouldAppendFinalTranscriptToTitleWhenDictating()
    {
        var page = NewDictationPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle)).ClickAsync(new());
        await component.InvokeAsync(() =>
            component.Instance.SpeechRecognition._OnResult(WorkOrderManageSteps.FinalTranscript("broken window")));

        await component.WaitForAssertionAsync(() =>
            component.Instance.Model.Title.ShouldBe("Test title broken window"));
    }

    [Test]
    public async Task ShouldSetDescriptionFromTranscriptWhenFieldIsEmpty()
    {
        var page = NewDictationPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateDescription)).ClickAsync(new());
        await component.InvokeAsync(() =>
            component.Instance.SpeechRecognition._OnResult(WorkOrderManageSteps.FinalTranscript("broken window")));

        await component.WaitForAssertionAsync(() =>
            component.Instance.Model.Description.ShouldBe("broken window"));
    }

    [Test]
    public async Task ShouldIgnoreInterimResultsWhenDictating()
    {
        var page = NewDictationPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle)).ClickAsync(new());
        await component.InvokeAsync(() =>
            component.Instance.SpeechRecognition._OnResult(WorkOrderManageSteps.InterimTranscript("broken window")));

        await component.WaitForAssertionAsync(() =>
            component.Instance.Model.Title.ShouldBe("Test title"));
    }

    [Test]
    public async Task ShouldResetListeningStateWhenRecognitionEnds()
    {
        var page = NewDictationPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle)).ClickAsync(new());
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle))
                .GetAttribute("aria-pressed").ShouldBe("true"));

        await component.InvokeAsync(() => component.Instance.SpeechRecognition._OnEnd());

        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle))
                .GetAttribute("aria-pressed").ShouldBe("false"));
    }

    [Test]
    public async Task ShouldHideDictateButtonsWhenWorkOrderIsReadOnly()
    {
        var user = WorkOrderManageSteps.Viewer();
        var creator = EmployeeBuilder.Create("someoneelse")
            .WithFirstName("Someone").WithLastName("Else").WithEmail("se@example.com")
            .WithId(Guid.NewGuid()).Build();
        var completed = WorkOrderBuilder.Create()
            .WithId(Guid.NewGuid()).WithNumber("WO-DONE").WithStatus(WorkOrderStatus.Complete)
            .CreatedBy(creator).AssignedTo(creator).WithTitle("Completed work order").Build();
        var page = WorkOrderManagePageContext.Create()
            .SignedInAs(user).Editing(completed).WithLooseJsInterop()
            .WithNewOrder(o => o.WithTitle("Test title").WithNumber("WO-TEST"));
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakTitle)).ShouldNotBeNull();
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle)).ShouldBeEmpty();
            component.FindAll(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateDescription)).ShouldBeEmpty();
        });
    }

    private static async Task AssertPressed(
        IRenderedComponent<WorkOrderManagePage> component, bool pressed, string cssClass)
    {
        await component.WaitForAssertionAsync(() =>
        {
            var element = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.DictateTitle));
            element.GetAttribute("aria-pressed").ShouldBe(pressed ? "true" : "false");
            element.GetAttribute("class").ShouldNotBeNull().ShouldContain(cssClass);
        });
    }

    private static WorkOrderManagePageContext NewDictationPage() =>
        WorkOrderManagePageContext.Create()
            .SignedInAs(WorkOrderManageSteps.Creator("es-ES"))
            .WithLooseJsInterop()
            .WithNewOrder(o => o.WithTitle("Test title").WithNumber("WO-TEST"));
}
