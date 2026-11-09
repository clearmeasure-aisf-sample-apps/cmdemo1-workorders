using Microsoft.Extensions.Caching.Memory;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// The responses remembered for idempotent requests, and which request each key was first used for, kept in the
/// memory cache for <paramref name="entryLifetime"/>.
/// </summary>
internal sealed class IdempotencyStore(IMemoryCache cache, TimeSpan entryLifetime)
{
    private const string BindingPrefix = "__idempotency_binding:";

    public IdempotentResponseSnapshot? Find(IdempotentRequest request) =>
        cache.TryGetValue(request.CompositeKey, out IdempotentResponseSnapshot? snapshot) ? snapshot : null;

    /// <summary>True when the key was first used for a different request (another method, path or body).</summary>
    public bool IsKeyUsedForAnotherRequest(IdempotentRequest request) =>
        cache.TryGetValue(BindingPrefix + request.Key, out string? boundComposite)
        && !string.Equals(boundComposite, request.CompositeKey, StringComparison.Ordinal);

    public void Remember(IdempotentRequest request, IdempotentResponseSnapshot snapshot)
    {
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = entryLifetime };
        cache.Set(request.CompositeKey, snapshot, options);
        cache.Set(BindingPrefix + request.Key, request.CompositeKey, options);
    }
}
