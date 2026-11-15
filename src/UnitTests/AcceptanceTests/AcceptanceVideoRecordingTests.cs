using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AcceptanceTests;

/// <summary>Characterization of <c>AcceptanceVideoRecording</c> (source contracts; no AcceptanceTests project ref).</summary>
public class AcceptanceVideoRecordingTests
{
    [Test]
    public void Source_WhenRead_GatesVideoBehindAcceptanceRecordVideoEnv()
    {
        var source = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/AcceptanceVideoRecording.cs");

        source.ShouldContain("ACCEPTANCE_RECORD_VIDEO");
        source.ShouldContain("RecordVideoDir");
        source.ShouldContain("playwright-videos");
        source.ShouldContain("internal static class AcceptanceVideoRecording");
        source.ShouldContain("internal static void ApplyTo");
        source.ShouldContain("ArgumentNullException.ThrowIfNull");
    }

    [Test]
    public void Source_WhenEnvDisabled_ApplyToReturnsBeforeCreatingVideoDir()
    {
        var source = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/AcceptanceVideoRecording.cs");

        source.ShouldContain("if (!IsEnabled())");
        source.ShouldContain("return;");
        var enabledGuard = source.IndexOf("if (!IsEnabled())", StringComparison.Ordinal);
        var createDir = source.IndexOf("Directory.CreateDirectory", StringComparison.Ordinal);
        enabledGuard.ShouldBeGreaterThan(-1);
        createDir.ShouldBeGreaterThan(enabledGuard);
    }

    [Test]
    public void Factory_WhenOpeningContext_DelegatesToAcceptanceVideoRecording()
    {
        var factory = RepositoryCheckout.Current.ReadText("src/AcceptanceTests/BrowserContextFactory.cs");

        factory.ShouldContain("AcceptanceVideoRecording.ApplyTo(options)");
        factory.ShouldNotContain("RecordVideoDir");
    }
}
