using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages.WorkOrderManage;

/// <summary>
/// Fake <see cref="IJSRuntime"/> that loads a clipboard module for copy-button tests.
/// </summary>
public sealed class WorkOrderManageClipboard : IJSRuntime, IJsInteropDouble
{
    private readonly ClipboardModule _module;

    /// <summary>Creates a clipboard runtime with optional failure modes.</summary>
    public WorkOrderManageClipboard(bool throwJsException = false, bool throwDisconnectedException = false)
    {
        _module = new ClipboardModule(throwJsException, throwDisconnectedException);
    }

    /// <summary>Last text passed to <c>copyToClipboard</c>.</summary>
    public string? LastCopiedText => _module.LastCopiedText;

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        JsInteropDouble.Answer<TValue>(this, identifier, args);

    ValueTask<TValue> IJsInteropDouble.Answer<TValue>(string identifier, object?[]? args)
    {
        if (identifier == "import" && typeof(TValue) == typeof(IJSObjectReference))
        {
            return ValueTask.FromResult((TValue)(object)_module);
        }

        throw new InvalidOperationException($"Unexpected InvokeAsync: {identifier}");
    }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
        object?[]? args) =>
        JsInteropDouble.Answer<TValue>(this, identifier, args);

    private sealed class ClipboardModule(bool throwJsException, bool throwDisconnectedException) : IJSObjectReference, IJsInteropDouble
    {
        public string? LastCopiedText { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            JsInteropDouble.Answer<TValue>(this, identifier, args);

        ValueTask<TValue> IJsInteropDouble.Answer<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "copyToClipboard")
            {
                throw new InvalidOperationException($"Unexpected InvokeAsync: {identifier}");
            }

            if (args is [string copied, ..])
            {
                LastCopiedText = copied;
            }

            if (throwJsException)
            {
                throw new JSException("Clipboard permission denied");
            }

            if (throwDisconnectedException)
            {
                throw new JSDisconnectedException("JS runtime disconnected");
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
            JsInteropDouble.Answer<TValue>(this, identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
