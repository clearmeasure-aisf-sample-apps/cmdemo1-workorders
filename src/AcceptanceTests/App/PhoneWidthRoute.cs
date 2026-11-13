namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>One 390px width assertion target.</summary>
public sealed class PhoneWidthRoute
{
    /// <summary>How <see cref="PhoneWidthNoDocumentScrollTests"/> waits for a route.</summary>
    public enum ReadyKind
    {
        /// <summary>Wait for a <c>data-testid</c>.</summary>
        TestId,

        /// <summary>Wait for a CSS selector.</summary>
        Css,

        /// <summary>Wait for an accessible heading name.</summary>
        Heading
    }

    /// <summary>Stable case key.</summary>
    public required string Key { get; init; }

    /// <summary>URL or template with <c>{number}</c>.</summary>
    public required string UrlTemplate { get; init; }

    /// <summary>When true, sign in before asserting.</summary>
    public bool RequiresSignIn { get; init; }

    /// <summary>When true, seed a work order and substitute <c>{number}</c>.</summary>
    public bool NeedsWorkOrderNumber { get; init; }

    /// <summary>When true, switch to dark theme before asserting.</summary>
    public bool UseDarkTheme { get; init; }

    /// <summary>Ready-wait strategy.</summary>
    public ReadyKind WaitKind { get; init; }

    /// <summary>Test id, CSS selector, or heading name.</summary>
    public required string ReadyToken { get; init; }

    /// <summary>Optional region that must scroll inside itself.</summary>
    public string? InnerScrollSelector { get; init; }

    /// <summary>When true, assert the top bar wraps at 390px.</summary>
    public bool AssertTopBarWraps { get; init; }
}
