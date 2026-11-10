using ClearMeasure.Bootcamp.Core.Services;
using Toolbelt.Blazor.SpeechSynthesis;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

/// <summary>
/// Reads a work-order field aloud in the user's language: translated first, then spoken with a voice of that
/// language when the browser has one. Speech synthesis may be missing; reading aloud then does nothing.
/// </summary>
internal sealed class WorkOrderReadAloud(ITranslationService? translationService, SpeechSynthesis? speechSynthesis)
{
    public async Task SpeakAsync(string? text, string language)
    {
        if (string.IsNullOrEmpty(text) || speechSynthesis == null || translationService == null)
        {
            return;
        }

        var translatedText = await translationService.TranslateAsync(text, language);
        try
        {
            var voices = await speechSynthesis.GetVoicesAsync();
            await speechSynthesis.SpeakAsync(CreateUtterance(translatedText, language, voices));
        }
        catch
        {
            // Speech synthesis may not be available in all environments
        }
    }

    public static SpeechSynthesisUtterance CreateUtterance(
        string translatedText,
        string language,
        IEnumerable<SpeechSynthesisVoice> voices)
    {
        var utterance = new SpeechSynthesisUtterance { Text = translatedText, Lang = language };
        var languagePrefix = language.Split('-')[0];
        var matchingVoice = voices.FirstOrDefault(voice =>
            voice.Lang.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase));
        if (matchingVoice != null)
        {
            utterance.Voice = matchingVoice;
        }

        return utterance;
    }
}
