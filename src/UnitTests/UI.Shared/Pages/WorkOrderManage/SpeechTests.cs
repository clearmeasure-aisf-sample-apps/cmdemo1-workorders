using Bunit;
using Shouldly;
using WorkOrderManagePage = ClearMeasure.Bootcamp.UI.Shared.Pages.WorkOrderManage;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

[TestFixture]
public class SpeechTests
{
    [Test]
    public async Task ShouldRenderSpeakTitleButton()
    {
        var page = NewSpeechPage("es-ES");
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var element = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakTitle));
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
        });
    }

    [Test]
    public async Task ShouldRenderSpeakDescriptionButton()
    {
        var page = NewSpeechPage();
        await using var ctx = page.Build();
        var component = page.Render(ctx);

        await component.WaitForAssertionAsync(() =>
        {
            var element = component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakDescription));
            element.ShouldNotBeNull();
            element.TagName.ShouldBe("BUTTON", StringCompareShould.IgnoreCase);
            element.GetAttribute("type").ShouldBe("button");
        });
    }

    [Test]
    public async Task SpeakTitleButtonShouldInvokeTranslationService()
    {
        var page = NewSpeechPage("es-ES");
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title)).ShouldNotBeNull());
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Title))
            .ChangeAsync(new() { Value = "Test title" });

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakTitle)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            page.Translation.LastText.ShouldBe("Test title");
            page.Translation.LastTargetLanguage.ShouldBe("es-ES");
        });
    }

    [Test]
    public async Task SpeakDescriptionButtonShouldInvokeTranslationService()
    {
        var page = NewSpeechPage("fr-FR");
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Description)).ShouldNotBeNull());
        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.Description))
            .ChangeAsync(new() { Value = "Test description" });

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakDescription)).ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            page.Translation.LastText.ShouldBe("Test description");
            page.Translation.LastTargetLanguage.ShouldBe("fr-FR");
        });
    }

    [Test]
    public async Task SpeakTitleButton_WhenTitleEmpty_DoesNotInvokeTranslationService()
    {
        var page = NewSpeechPage("es-ES", emptyTitle: true);
        await using var ctx = page.Build();
        var component = page.Render(ctx);
        await component.WaitForAssertionAsync(() =>
            component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakTitle)).ShouldNotBeNull());

        await component.Find(WorkOrderManageSteps.TestId(WorkOrderManagePage.Elements.SpeakTitle)).ClickAsync(new());

        page.Translation.LastText.ShouldBeNull();
    }

    private static WorkOrderManagePageContext NewSpeechPage(string? preferredLanguage = null, bool emptyTitle = false)
    {
        var user = preferredLanguage == null
            ? WorkOrderManageSteps.Viewer()
            : WorkOrderManageSteps.Creator(preferredLanguage);
        return WorkOrderManagePageContext.Create()
            .SignedInAs(user)
            .TranslationReturns()
            .WithNewOrder(o => o.WithTitle(emptyTitle ? "" : "Test title").WithNumber("WO-TEST"));
    }
}
