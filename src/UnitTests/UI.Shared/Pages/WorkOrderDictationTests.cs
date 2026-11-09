using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;
using Toolbelt.Blazor.SpeechRecognition;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderDictationTests
{
    [Test]
    public void ShouldJoinOnlyTheFinalTranscripts_WhenARecognitionResultArrives()
    {
        var args = new SpeechRecognitionEventArgs
        {
            ResultIndex = 1,
            Results =
            [
                Result(isFinal: true, " already handled "),
                Result(isFinal: true, " broken "),
                Result(isFinal: false, "maybe"),
                Result(isFinal: true, " window ")
            ]
        };

        var transcript = WorkOrderDictation.FinalTranscript(args);

        transcript.ShouldBe("broken window");
    }

    [Test]
    public async Task ShouldStayIdle_WhenTheBrowserHasNoSpeechRecognition()
    {
        await using var dictation = new WorkOrderDictation(null, (_, _) => { }, () => Task.CompletedTask);

        await dictation.ToggleAsync(DictationTarget.Title, "en-US");

        dictation.Target.ShouldBe(DictationTarget.None);
        dictation.IsListeningTo(DictationTarget.Title).ShouldBeFalse();
    }

    private static SpeechRecognitionResult Result(bool isFinal, string transcript) => new()
    {
        IsFinal = isFinal,
        Items = [new SpeechRecognitionAlternative { Transcript = transcript, Confidence = 0.9 }]
    };
}
