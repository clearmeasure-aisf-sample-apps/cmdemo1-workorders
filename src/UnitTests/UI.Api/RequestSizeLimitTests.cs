using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http.Metadata;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class RequestSizeLimitTests
{
    [Test]
    public void MaxRequestBodySize_Should_BeReadByKestrelThroughTheRequestSizeLimitMetadata()
    {
        IRequestSizeLimitMetadata metadata = new RequestSizeLimit(1_048_576);

        metadata.MaxRequestBodySize.ShouldBe(1_048_576);
    }
}
