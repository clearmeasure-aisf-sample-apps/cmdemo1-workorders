using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class PlainTextTests
{
    [Test]
    public void Ok_Should_ReturnUtf8PlainTextWith200()
    {
        var result = PlainText.Ok("pong");

        result.StatusCode.ShouldBe(200);
        result.ContentType.ShouldBe("text/plain; charset=utf-8");
        result.ResponseContent.ShouldBe("pong");
    }
}
