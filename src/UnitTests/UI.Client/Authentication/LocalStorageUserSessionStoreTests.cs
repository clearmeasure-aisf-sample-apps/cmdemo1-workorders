using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

[TestFixture]
public class LocalStorageUserSessionStoreTests
{
    private const string StorageKey = "bootcamp.userSession.username";

    [Test]
    public async Task ClearAsync_AfterSet_ShouldYieldNull()
    {
        var js = new StubLocalStorageJsRuntime();
        var store = new LocalStorageUserSessionStore(js);
        await store.SetAsync("tlovejoy");

        await store.ClearAsync();

        (await store.GetAsync()).ShouldBeNullOrEmpty();
        js.RemoveItemCalls.ShouldBe(1);
        js.GetItemCalls.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ClearAsync_ShouldThrow_WhenGetItemStillReturnsUsername()
    {
        var js = new StubLocalStorageJsRuntime { RefuseToRemove = true };
        var store = new LocalStorageUserSessionStore(js);
        await store.SetAsync("tlovejoy");

        var ex = await Should.ThrowAsync<InvalidOperationException>(store.ClearAsync);

        ex.Message.ShouldContain(StorageKey);
        (await store.GetAsync()).ShouldBe("tlovejoy");
    }

    [Test]
    public async Task ClearAsync_WhenReadbackFails_ShouldThrowWithStorageContext()
    {
        var jsException = new JSException("localStorage unavailable");
        var js = new StubLocalStorageJsRuntime { GetItemException = jsException };
        var store = new LocalStorageUserSessionStore(js);

        var ex = await Should.ThrowAsync<InvalidOperationException>(store.ClearAsync);

        ex.Message.ShouldContain(StorageKey);
        ex.InnerException.ShouldBe(jsException);
    }

    [Test]
    public async Task SetAsync_WhenStorageInteropFails_ShouldThrowWithStorageContext()
    {
        var jsException = new JSException("localStorage unavailable");
        var js = new StubLocalStorageJsRuntime { SetItemException = jsException };
        var store = new LocalStorageUserSessionStore(js);

        var ex = await Should.ThrowAsync<InvalidOperationException>(() => store.SetAsync("tlovejoy"));

        ex.Message.ShouldContain(StorageKey);
        ex.InnerException.ShouldBe(jsException);
    }

    private sealed class StubLocalStorageJsRuntime : IJSRuntime, IJsInteropDouble
    {
        private string? _value;

        public bool RefuseToRemove { get; init; }
        public JSException? GetItemException { get; init; }
        public JSException? SetItemException { get; init; }
        public int RemoveItemCalls { get; private set; }
        public int GetItemCalls { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            JsInteropDouble.Answer<TValue>(this, identifier, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
            JsInteropDouble.Answer<TValue>(this, identifier, args);

        ValueTask<TValue> IJsInteropDouble.Answer<TValue>(string identifier, object?[]? args)
        {
            switch (identifier)
            {
                case "localStorage.getItem":
                    GetItemCalls++;
                    return GetItemException is null
                        ? ValueTask.FromResult((TValue)(object?)_value!)
                        : throw GetItemException;
                case "localStorage.setItem":
                    _value = SetItemException is null ? (string?)args![1] : throw SetItemException;
                    return ValueTask.FromResult(default(TValue)!);
                case "localStorage.removeItem":
                    RemoveItemCalls++;
                    _value = RefuseToRemove ? _value : null;
                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new InvalidOperationException($"Unexpected InvokeAsync: {identifier}");
            }
        }
    }
}
