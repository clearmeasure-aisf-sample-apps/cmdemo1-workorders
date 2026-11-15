using Microsoft.JSInterop;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

/// <summary>
/// Test double that throws <see cref="JSDisconnectedException"/> on module import or on dispose of a loaded module.
/// </summary>
internal sealed class DisconnectingJsRuntime : IJSRuntime
{
    private readonly bool _failImport;
    private readonly bool _failDispose;

    public DisconnectingJsRuntime(bool failImport = false, bool failDispose = false)
    {
        _failImport = failImport;
        _failDispose = failDispose;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier == "import")
        {
            if (_failImport)
                throw new JSDisconnectedException("circuit disconnected");

            return ValueTask.FromResult((TValue)(object)new DisconnectingModule(_failDispose));
        }

        return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
        object?[]? args) =>
        InvokeAsync<TValue>(identifier, args);

    private sealed class DisconnectingModule(bool failDispose) : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (typeof(TValue) == typeof(IJSObjectReference))
                return ValueTask.FromResult((TValue)(object)new DisconnectingModule(failDispose));

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            if (failDispose)
                throw new JSDisconnectedException("circuit disconnected");
            return ValueTask.CompletedTask;
        }
    }
}
