using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class WebServiceApiRoutesTests
{
    [Test]
    public void ShouldExposeAbstractPathSegment_When_Read()
    {
        WebServiceApiRoutes.AbstractPathSegment.ShouldBe("blazor-wasm-single-api");
    }

    [Test]
    public void ShouldPrefixLegacyUrlWithApi_When_Read()
    {
        WebServiceApiRoutes.LegacyRelativeUrl.ShouldBe("api/blazor-wasm-single-api");
    }

    [Test]
    public void ShouldPrefixVersionedUrlWithApiV1_When_Read()
    {
        WebServiceApiRoutes.VersionedRelativeUrl.ShouldBe("api/v1.0/blazor-wasm-single-api");
    }
}
