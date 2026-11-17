using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ApiJsonContextTests
{
    [Test]
    public void Options_Should_UseWebDefaults_So_PropertiesAreCamelCaseAndCaseInsensitive()
    {
        var options = ApiJsonContext.Default.Options;

        options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
        options.PropertyNameCaseInsensitive.ShouldBeTrue();
    }

    [Test]
    public void GetTypeInfo_Should_DescribeEveryToolBody_WithoutReflection()
    {
        Type[] bodies = [typeof(Base64Request), typeof(CaseRequest), typeof(HashRequest), typeof(SlugifyRequest),
            typeof(UppercaseRequest), typeof(WordCountRequest), typeof(WorkOrderStatusDto[])];

        bodies.Select(ApiJsonContext.Default.GetTypeInfo).ShouldAllBe(info => info != null);
    }
}
