using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

[TestFixture]
public class UiSharedConventionRulesTests
{
    [Test]
    public void NullableInjectPropertyNames_WhenSampleInjectIsNullable_ReportsTheProperty()
    {
        UiSharedConventionRules.NullableInjectPropertyNames("""
            using Microsoft.AspNetCore.Components;
            namespace Sample;
            public class Page {
              [Inject] public IUserSession? UserSession { get; set; }
            }
            """).ShouldBe(["UserSession"]);
    }

    [Test]
    public void NullableInjectProperties_WhenUiSharedIsScanned_AreAbsent()
    {
        var violations = UiSharedConventionScan.NullableInjectViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "[Inject] properties in UI.Shared must be non-nullable. Found: " + string.Join(", ", violations));
    }

    [Test]
    public void AuthenticationProviderInjections_WhenSampleInjectsProvider_ReportsTheType()
    {
        UiSharedConventionRules.AuthenticationProviderInjections("""
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Authorization;
            namespace Sample;
            public class Page {
              [Inject] public AuthenticationStateProvider Auth { get; set; } = null!;
            }
            """).ShouldBe(["AuthenticationStateProvider"]);
    }

    [Test]
    public void AuthenticationProviderInjections_WhenOutsideAllowList_AreAbsent()
    {
        var violations = UiSharedConventionScan.AuthenticationProviderViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "Only UI.Shared/Authentication and UI/Client may inject AuthenticationStateProvider. Found: " +
            string.Join(", ", violations));
    }

    [Test]
    public void ThrowsBareException_WhenSampleThrowsException_IsTrue()
    {
        UiSharedConventionRules.ThrowsBareException("""
            namespace Sample;
            public class Boom {
              public void Go() { throw new Exception("no"); }
            }
            """).ShouldBeTrue();
    }

    [Test]
    public void ThrowsBareException_WhenSampleThrowsDerived_IsFalse()
    {
        UiSharedConventionRules.ThrowsBareException("""
            namespace Sample;
            public class Boom {
              public void Go() { throw new InvalidOperationException("no"); }
            }
            """).ShouldBeFalse();
    }

    [Test]
    public void BareExceptionThrows_WhenProductionSourcesAreScanned_AreAbsent()
    {
        var violations = UiSharedConventionScan.BareExceptionViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "Production code must not throw new Exception(. Found: " + string.Join(", ", violations));
    }
}
