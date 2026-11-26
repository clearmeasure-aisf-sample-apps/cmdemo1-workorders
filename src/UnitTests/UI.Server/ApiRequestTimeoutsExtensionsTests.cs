using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiRequestTimeoutsExtensionsTests
{
    [Test]
    public void Should_AddApiControllersPolicy_When_TimeoutsEnabled()
    {
        var options = ResolveOptions("true", "5");

        var policy = options.Policies[ApiRequestTimeoutsExtensions.ApiControllersPolicyName];

        policy.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        policy.TimeoutStatusCode.ShouldBe(StatusCodes.Status504GatewayTimeout);
    }

    [TestCase("false", "5")]
    [TestCase("true", "0")]
    public void Should_SkipApiControllersPolicy_When_DisabledOrTimeoutNotPositive(string enabled, string seconds)
    {
        var options = ResolveOptions(enabled, seconds);

        options.Policies.ContainsKey(ApiRequestTimeoutsExtensions.ApiControllersPolicyName).ShouldBeFalse();
    }

    private static RequestTimeoutOptions ResolveOptions(string enabled, string seconds)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ApiRequestTimeoutOptions.SectionName}:Enabled"] = enabled,
                [$"{ApiRequestTimeoutOptions.SectionName}:TimeoutSeconds"] = seconds
            })
            .Build();
        return new ServiceCollection()
            .AddApiRequestTimeouts(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<RequestTimeoutOptions>>()
            .Value;
    }
}
