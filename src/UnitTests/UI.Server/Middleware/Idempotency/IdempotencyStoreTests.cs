using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware.Idempotency;

[TestFixture]
public class IdempotencyStoreTests
{
    private static readonly IdempotentRequest First = new("k1", "POST|/api/items|k1|AAA");
    private static readonly IdempotentRequest SameKeyOtherBody = new("k1", "POST|/api/items|k1|BBB");
    private static readonly IdempotentResponseSnapshot Created = new(201, [], "created"u8.ToArray());

    [Test]
    public void ShouldFindTheRememberedResponse_WhenTheSameRequestComesAgain()
    {
        var store = new IdempotencyStore(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromMinutes(1));

        store.Remember(First, Created);

        store.Find(First).ShouldBe(Created);
        store.Find(SameKeyOtherBody).ShouldBeNull();
    }

    [Test]
    public void ShouldReportTheKeyAsTaken_WhenItWasFirstUsedForAnotherRequest()
    {
        var store = new IdempotencyStore(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromMinutes(1));

        store.Remember(First, Created);

        store.IsKeyUsedForAnotherRequest(SameKeyOtherBody).ShouldBeTrue();
        store.IsKeyUsedForAnotherRequest(First).ShouldBeFalse();
    }

    [Test]
    public void ShouldReportTheKeyAsFree_WhenNothingWasRemembered()
    {
        var store = new IdempotencyStore(new MemoryCache(new MemoryCacheOptions()), TimeSpan.FromMinutes(1));

        var taken = store.IsKeyUsedForAnotherRequest(First);

        taken.ShouldBeFalse();
    }
}
