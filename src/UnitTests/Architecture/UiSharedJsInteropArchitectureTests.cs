using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// UI.Shared JS interop conventions: empty catches only in allow-listed script classes, no
/// <c>IJSObjectReference</c> in <c>.razor.cs</c>, and every silent catch-all logs (or rethrows).
/// </summary>
[TestFixture]
public class UiSharedJsInteropArchitectureTests
{
    [Test]
    public void UiShared_ShouldNotHaveEmptyCatchOutsideScriptClasses_WhenSourcesAreScanned()
    {
        var violations = UiSharedConventionScan.EmptyCatchViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "Empty catch blocks belong only in allow-listed *Script.cs (circuit disconnect). Found: " +
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
    public void UiShared_SilentCatchAlls_ShouldLogOrRethrow_WhenSourcesAreScanned()
    {
        var violations = UiSharedConventionScan.CatchExceptionWithoutLogViolations(RepositoryCheckout.Current);
        violations.ShouldBeEmpty(
            "catch / catch (Exception) in UI.Shared must LogWarning (or rethrow). Found: " +
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
    public void EmptyCatchLineNumbers_WhenRazorCodeHasEmptyCatch_ReportsTheLine()
    {
        UiSharedConventionRules.EmptyCatchLineNumbers("""
            @page "/x"
            @code {
              void Go() {
                try { }
                catch (JSDisconnectedException) { }
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

    [Test]
    public void CatchExceptionWithoutLogLineNumbers_WhenBareCatchHasNoLog_ReportsTheLine()
    {
        UiSharedConventionRules.CatchExceptionWithoutLogLineNumbers("""
            namespace Sample;
            public class C {
              public void Go() {
                try { }
                catch { _ = 1; }
              }
            }
            """).ShouldNotBeEmpty();
    }

    [Test]
    public void CatchExceptionWithoutLogLineNumbers_WhenCatchRethrows_ReportsNothing()
    {
        UiSharedConventionRules.CatchExceptionWithoutLogLineNumbers("""
            namespace Sample;
            public class C {
              public void Go() {
                try { }
                catch (System.Exception) { throw; }
              }
            }
            """).ShouldBeEmpty();
    }

    [Test]
    public void CatchExceptionWithoutLogLineNumbers_WhenRazorCatchHasNoLog_ReportsTheLine()
    {
        UiSharedConventionRules.CatchExceptionWithoutLogLineNumbers("""
            @page "/x"
            @code {
              void Go() {
                try { }
                catch (Exception) { _x = null; }
              }
              object? _x;
            }
            """).ShouldNotBeEmpty();
    }
}
