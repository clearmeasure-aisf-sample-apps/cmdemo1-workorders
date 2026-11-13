namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>One axe scan target.</summary>
public sealed class AccessibilityAxeRoute
{
    /// <summary>How <see cref="AccessibilityAxeTests"/> waits for a route before scanning.</summary>
    public enum ReadyKind
    {
        /// <summary>Wait for a <c>data-testid</c>.</summary>
        TestId,

        /// <summary>Wait until present selector exists and absent selector is gone.</summary>
        PresentAbsent
    }

    /// <summary>Stable case key.</summary>
    public required string Key { get; init; }

    /// <summary>URL or template with <c>{number}</c>.</summary>
    public required string UrlTemplate { get; init; }

    /// <summary>When true, sign in before scanning.</summary>
    public bool RequiresSignIn { get; init; }

    /// <summary>When true, seed a work order and substitute <c>{number}</c>.</summary>
    public bool NeedsWorkOrderNumber { get; init; }

    /// <summary>Ready-wait strategy.</summary>
    public ReadyKind WaitKind { get; init; }

    /// <summary>Test id, or <c>present|absent</c> for PresentAbsent.</summary>
    public required string ReadyToken { get; init; }
}
