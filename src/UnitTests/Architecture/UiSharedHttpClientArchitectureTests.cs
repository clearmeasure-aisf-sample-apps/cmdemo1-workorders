using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// UI.Shared must not inject or construct <see cref="HttpClient"/> (empty allow-list).
/// </summary>
[TestFixture]
public class UiSharedHttpClientArchitectureTests
{
    [Test]
    public void UiShared_ShouldNotInjectOrConstructHttpClient_WhenSourcesAreScanned()
    {
        var violations = UiSharedConventionScan.HttpClientViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "UI.Shared types may not inject or construct HttpClient. Found: " + string.Join(", ", violations));
    }

    [Test]
    public void HttpClientInjections_WhenSampleInjectsHttpClient_ReportsTheMember()
    {
        UiSharedConventionRules.HttpClientInjections("""
            using Microsoft.AspNetCore.Components;
            namespace Sample;
            public class Page {
              [Inject] public HttpClient Http { get; set; } = null!;
            }
            """).ShouldBe(["Http"]);
    }

    [Test]
    public void HttpClientConstructions_WhenSampleNewsHttpClient_ReportsTheType()
    {
        UiSharedConventionRules.HttpClientConstructions("""
            namespace Sample;
            public class Model {
              public static HttpClient Create() => new HttpClient();
            }
            """).ShouldContain("HttpClient");
    }
}
