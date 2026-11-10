using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class SearchPageScriptTests
{
    private const string ModulePath = "./_content/ClearMeasure.Bootcamp.UI.Shared/js/searchExport.js";

    [Test]
    public async Task ShouldHandTheFileNameAndCsvToTheScript_WhenDownloading()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("downloadCsv", "work-orders.csv", "Number\n101").SetVoidResult();
        var script = new SearchPageScript(ctx.JSInterop.JSRuntime);

        await script.DownloadCsvAsync("work-orders.csv", "Number\n101");

        module.VerifyInvoke("downloadCsv");
    }

    [Test]
    public async Task ShouldImportTheModuleOnce_WhenCalledTwice()
    {
        await using var ctx = new BunitContext();
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("focusById", _ => true).SetVoidResult();
        var script = new SearchPageScript(ctx.JSInterop.JSRuntime);
        await script.FocusByIdAsync("row-menu-1");

        await script.FocusByIdAsync("row-menu-2");

        ctx.JSInterop.VerifyInvoke("import", calledTimes: 1);
    }
}
