using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// Configurable <see cref="ITranslationService"/> that echoes input and records the last call.
/// </summary>
public sealed class WorkOrderManageTranslation : ITranslationService
{
    /// <summary>Last text passed to <see cref="TranslateAsync"/>, if any.</summary>
    public string? LastText { get; private set; }

    /// <summary>Last target language passed to <see cref="TranslateAsync"/>, if any.</summary>
    public string? LastTargetLanguage { get; private set; }

    /// <inheritdoc />
    public Task<string> TranslateAsync(string text, string targetLanguageCode)
    {
        LastText = text;
        LastTargetLanguage = targetLanguageCode;
        return Task.FromResult(text);
    }
}
