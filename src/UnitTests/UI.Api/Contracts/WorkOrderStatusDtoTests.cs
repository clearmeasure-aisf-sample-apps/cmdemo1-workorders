using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api.Contracts;

/// <summary>Pins the response body the API writes through its source-generated <see cref="ApiJsonContext"/> (Native AOT).</summary>
[TestFixture]
public class WorkOrderStatusDtoTests
{
    [Test]
    public void Serialize_Should_WriteEachStatusInCamelCase_When_WrittenAsArray()
    {
        WorkOrderStatusDto[] statuses = [new("DFT", "Draft", "Draft", 0)];

        var json = JsonSerializer.Serialize(statuses, ApiJsonContext.Default.WorkOrderStatusDtoArray);

        json.ShouldBe("""[{"code":"DFT","key":"Draft","friendlyName":"Draft","sortBy":0}]""");
    }
}
