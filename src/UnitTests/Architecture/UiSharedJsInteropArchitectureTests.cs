using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// UI.Shared JS interop conventions: empty catches only in <c>*Script.cs</c>, no
/// <c>IJSObjectReference</c> in <c>.razor.cs</c>, and every <c>catch (Exception)</c> logs.
/// </summary>
[TestFixture]
public class UiSharedJsInteropArchitectureTests
{
    [Test]
    public void UiShared_ShouldNotHaveEmptyCatchOutsideScriptClasses_WhenSourcesAreScanned()
    {
        var violations = UiSharedConventionScan.EmptyCatchViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "Empty catch blocks belong only in *Script.cs (circuit disconnect). Found: " +
            string.Join(", ", violations));
    }

    [Test]
    public void UiShared_ShouldNotUseIjsObjectReferenceInRazorCodeBehinds_WhenSourcesAreScanned()
    {
        var violations = UiSharedConventionScan.RazorCodeBehindIjsObjectReferenceViolations(
            RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "IJSObjectReference belongs in *Script.cs, not .razor.cs. Found: " +
            string.Join(", ", violations));
    }

    [Test]
    public void UiShared_CatchExceptionBlocks_ShouldLog_WhenSourcesAreScanned()
    {
        var violations = UiSharedConventionScan.CatchExceptionWithoutLogViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "catch (Exception) in UI.Shared must LogWarning (or Error/Critical). Found: " +
            string.Join(", ", violations));
    }

    [Test]
    public void EmptyCatchLineNumbers_WhenSampleHasEmptyCatch_ReportsTheLine()
    {
        UiSharedConventionRules.EmptyCatchLineNumbers("""
            namespace Sample;
            public class C {
              public void Go() {
                try { }
                catch (System.Exception) { }
              }
            }
            """).ShouldNotBeEmpty();
    }

    [Test]
    public void EmptyCatchLineNumbers_WhenCatchLogs_ReportsNothing()
    {
        UiSharedConventionRules.EmptyCatchLineNumbers("""
            namespace Sample;
            public class C {
              public void Go(Microsoft.Extensions.Logging.ILogger log) {
                try { }
                catch (System.Exception ex) { log.LogWarning(ex, "x"); }
              }
            }
            """).ShouldBeEmpty();
    }

    [Test]
    public void UsesIjsObjectReference_WhenSampleDeclaresField_IsTrue()
    {
        UiSharedConventionRules.UsesIjsObjectReference("""
            using Microsoft.JSInterop;
            namespace Sample;
            public class C { private IJSObjectReference? _m; }
            """).ShouldBeTrue();
    }

    [Test]
    public void CatchExceptionWithoutLogLineNumbers_WhenCatchReturnsWithoutLog_ReportsTheLine()
    {
        UiSharedConventionRules.CatchExceptionWithoutLogLineNumbers("""
            namespace Sample;
            public class C {
              public bool Go() {
                try { return true; }
                catch (System.Exception) { return false; }
              }
            }
            """).ShouldNotBeEmpty();
    }
}
