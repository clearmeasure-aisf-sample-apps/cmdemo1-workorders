using Microsoft.JSInterop;
using Toolbelt.Blazor.SpeechRecognition;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Dictation into one work-order field at a time: toggling a field starts or stops the browser's speech recognition
/// in the user's language, and each final transcript is handed to <paramref name="onTranscript"/> for that field.
/// Speech recognition may be missing or fail in some browsers; dictation then simply does not start.
/// </summary>
internal sealed class WorkOrderDictation : IAsyncDisposable
{
    private readonly SpeechRecognition? _recognition;
    private readonly Action<DictationTarget, string> _onTranscript;
    private readonly Func<Task> _onChanged;

    public WorkOrderDictation(SpeechRecognition? recognition, Action<DictationTarget, string> onTranscript, Func<Task> onChanged)
    {
        _recognition = recognition;
        _onTranscript = onTranscript;
        _onChanged = onChanged;
        if (recognition != null)
        {
            recognition.Result += OnResult;
            recognition.End += OnEnd;
        }
    }

    public DictationTarget Target { get; private set; } = DictationTarget.None;

    public bool IsListeningTo(DictationTarget target) => Target == target;

    public async Task ToggleAsync(DictationTarget target, string language)
    {
        if (_recognition == null)
        {
            return;
        }

        if (Target == target)
        {
            await StopAsync();
            return;
        }

        await StartAsync(target, language);
    }

    /// <summary>The final transcripts of a recognition result, trimmed and joined with spaces.</summary>
    public static string FinalTranscript(SpeechRecognitionEventArgs args) =>
        string.Join(" ", (args.Results ?? [])
            .Skip(args.ResultIndex)
            .Where(result => result.IsFinal)
            .Select(result => result.Items?.FirstOrDefault()?.Transcript?.Trim())
            .Where(transcript => !string.IsNullOrEmpty(transcript)));

    public async ValueTask DisposeAsync()
    {
        if (_recognition == null)
        {
            return;
        }

        _recognition.Result -= OnResult;
        _recognition.End -= OnEnd;
        try
        {
            if (Target != DictationTarget.None)
            {
                await _recognition.StopAsync();
            }

            await _recognition.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task StopAsync()
    {
        Target = DictationTarget.None;
        try
        {
            await _recognition!.StopAsync();
        }
        catch
        {
            // Speech recognition may not be available in all environments
        }
    }

    private async Task StartAsync(DictationTarget target, string language)
    {
        _recognition!.Lang = language;
        _recognition.Continuous = false;
        _recognition.InterimResults = false;
        Target = target;
        try
        {
            await _recognition.StartAsync();
        }
        catch
        {
            // Speech recognition may not be available in all environments
            Target = DictationTarget.None;
        }
    }

    private void OnResult(object? sender, SpeechRecognitionEventArgs args)
    {
        var transcript = FinalTranscript(args);
        if (string.IsNullOrEmpty(transcript))
        {
            return;
        }

        _onTranscript(Target, transcript);
        _ = _onChanged();
    }

    private void OnEnd(object? sender, EventArgs e)
    {
        Target = DictationTarget.None;
        _ = _onChanged();
    }
}
