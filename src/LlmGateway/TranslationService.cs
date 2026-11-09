using ClearMeasure.Bootcamp.Core.Services;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class TranslationService(ChatClientFactory chatClientFactory) : ITranslationService
{
    public async Task<string> TranslateAsync(string text, string targetLanguageCode)
    {
        if (TranslationGuard.ShouldReturnOriginal(text, targetLanguageCode)
            || await TryGetChatClientAsync() is not { } chatClient)
        {
            return text;
        }

        var response = await chatClient.GetResponseAsync(TranslationPrompt(text, targetLanguageCode));
        var translatedText = response.Text.Trim();
        return string.IsNullOrWhiteSpace(translatedText) ? text : translatedText;
    }

    /// <summary>The chat client, or <c>null</c> when none can be created: translation then returns the original text.</summary>
    private async Task<IChatClient?> TryGetChatClientAsync()
    {
        try
        {
            return await chatClientFactory.GetChatClient();
        }
        catch
        {
            return null;
        }
    }

    private static List<ChatMessage> TranslationPrompt(string text, string targetLanguageCode) =>
    [
        new(ChatRole.System,
            $"Translate the following text into the language identified by BCP 47 code '{targetLanguageCode}'. Return ONLY the translated text, nothing else."),
        new(ChatRole.User, text)
    ];
}
