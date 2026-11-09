using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware.Idempotency;

[TestFixture]
public class IdempotentRequestTests
{
    [TestCase("GET", "/api/items", false)]
    [TestCase("POST", "/api/items", true)]
    [TestCase("PUT", "/api/items/1", true)]
    [TestCase("POST", "/api/blazor-wasm-single-api", true)]
    [TestCase("POST", "/home", false)]
    public void ShouldInspectOnlyApiWrites_WhenARequestArrives(string method, string path, bool expected)
    {
        var request = IdempotencyMother.Request(method, path);

        var inspect = IdempotentRequest.ShouldInspect(request);

        inspect.ShouldBe(expected);
    }

    [Test]
    public void ShouldReadNoKey_WhenTheHeaderIsMissing()
    {
        var request = IdempotencyMother.Request("POST", "/api/items");

        var key = IdempotentRequest.ReadKey(request);

        key.ShouldBeNull();
    }

    [Test]
    public void ShouldReadTheTrimmedKey_WhenTheHeaderIsPresent()
    {
        var request = IdempotencyMother.Request("POST", "/api/items");
        request.Headers["Idempotency-Key"] = "  abc-123  ";

        var key = IdempotentRequest.ReadKey(request);

        key.ShouldBe("abc-123");
    }

    [Test]
    public async Task ShouldBuildTheSameCompositeKey_WhenTheSameBodyIsSentTwice()
    {
        var first = await IdempotentRequest.ForAsync(IdempotencyMother.Post("payload"), "k1", CancellationToken.None);
        var second = await IdempotentRequest.ForAsync(IdempotencyMother.Post("payload"), "k1", CancellationToken.None);

        first.ShouldBe(second);
        first.CompositeKey.ShouldContain("k1");
    }

    [Test]
    public async Task ShouldBuildAnotherCompositeKey_WhenTheBodyDiffers()
    {
        var first = await IdempotentRequest.ForAsync(IdempotencyMother.Post("payload"), "k1", CancellationToken.None);
        var second = await IdempotentRequest.ForAsync(IdempotencyMother.Post("other"), "k1", CancellationToken.None);

        first.CompositeKey.ShouldNotBe(second.CompositeKey);
    }

    [Test]
    public async Task ShouldLeaveTheBodyReadable_WhenTheKeyIsBuilt()
    {
        var request = IdempotencyMother.Post("payload");

        await IdempotentRequest.ForAsync(request, "k1", CancellationToken.None);

        (await new StreamReader(request.Body).ReadToEndAsync()).ShouldBe("payload");
    }
}
