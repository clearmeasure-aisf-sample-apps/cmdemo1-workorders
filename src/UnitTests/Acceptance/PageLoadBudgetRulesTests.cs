using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

/// <summary>
/// Proves the page-load budget rule without a browser. The live measurement uses this same type.
/// </summary>
[TestFixture]
public class PageLoadBudgetRulesTests
{
    private const string App = "https://localhost:7174";

    [Test]
    public void Evaluate_ShouldFail_WhenFiftyOneApiCallsExceedMaxOfFive()
    {
        var requests = new PageLoadBudgetRules.RecordedRequest[51];
        for (var index = 0; index < requests.Length; index++)
        {
            requests[index] = new PageLoadBudgetRules.RecordedRequest($"{App}/api/items/{index}", "fetch");
        }

        var verdict = PageLoadBudgetRules.Evaluate(requests, App, minimumCalls: 1, maximumCalls: 5);

        verdict.Passed.ShouldBeFalse();
        verdict.ApiCallCount.ShouldBe(51);
        verdict.Detail.ShouldContain("above the maximum");
    }

    [Test]
    public void Evaluate_ShouldPass_WhenApiCallCountIsWithinBudget()
    {
        PageLoadBudgetRules.RecordedRequest[] requests =
        [
            new($"{App}/api/blazor-wasm-single-api", "fetch"),
            new($"{App}/api/work-orders", "xhr"),
            new($"{App}/API/employees", "fetch"),
            new($"{App}/api/teams", "fetch")
        ];

        var verdict = PageLoadBudgetRules.Evaluate(requests, App, minimumCalls: 3, maximumCalls: 5);

        verdict.Passed.ShouldBeTrue(verdict.Detail);
        verdict.ApiCallCount.ShouldBe(4);
    }

    [Test]
    public void Evaluate_ShouldPass_WhenCountEqualsTheMaximum()
    {
        var requests = new PageLoadBudgetRules.RecordedRequest[5];
        for (var index = 0; index < requests.Length; index++)
        {
            requests[index] = new PageLoadBudgetRules.RecordedRequest($"{App}/api/items/{index}", "fetch");
        }

        var verdict = PageLoadBudgetRules.Evaluate(requests, App, minimumCalls: 1, maximumCalls: 5);

        verdict.Passed.ShouldBeTrue(verdict.Detail);
        verdict.ApiCallCount.ShouldBe(5);
    }

    [Test]
    public void Evaluate_ShouldIgnoreNonApiRequests_WhenCounting()
    {
        PageLoadBudgetRules.RecordedRequest[] requests =
        [
            new($"{App}/", "document"),
            new($"{App}/search", "document"),
            new($"{App}/_framework/blazor.webassembly.js", "script"),
            new($"{App}/_framework/blazor.boot.json", "fetch"),
            new($"{App}/css/app.css", "stylesheet"),
            new($"{App}/favicon.png", "image"),
            new($"{App}/fonts/app.woff2", "font"),
            new("wss://localhost:7174/notifications", "websocket"),
            new("https://example.test/api/items", "fetch"),
            new($"{App}/application/home", "document"),
            new($"{App}/api/blazor-wasm-single-api", "fetch"),
            new($"{App}/api/work-orders/status-counts", "xhr")
        ];

        var verdict = PageLoadBudgetRules.Evaluate(requests, App, minimumCalls: 2, maximumCalls: 5);

        verdict.ApiCallCount.ShouldBe(2);
        verdict.Passed.ShouldBeTrue(verdict.Detail);
    }

    [Test]
    public void Evaluate_ShouldFail_WhenApiCallCountIsBelowMinimum()
    {
        PageLoadBudgetRules.RecordedRequest[] requests =
        [
            new($"{App}/_framework/blazor.webassembly.js", "script"),
            new($"{App}/api/blazor-wasm-single-api", "fetch"),
            new("wss://localhost:7174/notifications", "websocket"),
            new($"{App}/api/employees", "xhr")
        ];

        var verdict = PageLoadBudgetRules.Evaluate(requests, App, minimumCalls: 3, maximumCalls: 5);

        verdict.Passed.ShouldBeFalse();
        verdict.ApiCallCount.ShouldBe(2);
        verdict.Detail.ShouldContain("below the minimum");
    }
}
