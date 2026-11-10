using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware.Idempotency;

[TestFixture]
public class IdempotentResponseSnapshotTests
{
    [Test]
    public void ShouldKeepOnlyEndToEndHeaders_WhenAResponseIsCaptured()
    {
        var context = IdempotencyMother.ContextWithBufferedResponse();
        context.Response.StatusCode = 201;
        context.Response.Headers["Location"] = "/api/items/1";
        context.Response.Headers["Connection"] = "keep-alive";
        context.Response.Headers["Transfer-Encoding"] = "chunked";

        var snapshot = IdempotentResponseSnapshot.Capture(context.Response, "created"u8.ToArray());

        snapshot.StatusCode.ShouldBe(201);
        snapshot.Headers.Keys.ShouldBe(["Location"]);
        snapshot.Body.ShouldBe("created"u8.ToArray());
    }

    [Test]
    public async Task ShouldWriteTheStatusHeadersAndBody_WhenReplayed()
    {
        var snapshot = new IdempotentResponseSnapshot(
            201,
            new Dictionary<string, string[]> { ["Location"] = ["/api/items/1"], ["Content-Length"] = ["999"] },
            "created"u8.ToArray());
        var context = IdempotencyMother.ContextWithBufferedResponse();

        await snapshot.ReplayAsync(context);

        context.Response.StatusCode.ShouldBe(201);
        context.Response.Headers.Location.ToString().ShouldBe("/api/items/1");
        context.Response.ContentLength.ShouldBe(7);
        ((MemoryStream)context.Response.Body).ToArray().ShouldBe("created"u8.ToArray());
    }
}
