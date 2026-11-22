using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class ApiKeyConstantsTests
{
    [Test]
    public void ShouldUseXApiKeyHeader_When_Read()
    {
        ApiKeyConstants.HeaderName.ShouldBe("X-Api-Key");
    }
}
