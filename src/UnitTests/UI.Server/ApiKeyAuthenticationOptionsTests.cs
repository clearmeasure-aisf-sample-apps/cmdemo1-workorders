using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyAuthenticationOptionsTests
{
    [TestCase(true, "secret", true)]
    [TestCase(false, "secret", false)]
    [TestCase(true, null, false)]
    [TestCase(true, "", false)]
    [TestCase(true, "   ", false)]
    public void Should_ReportConfigured_When_EnabledAndKeyAreSet(bool enabled, string? validationKey, bool expected)
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = enabled, ValidationKey = validationKey };

        options.IsConfigured.ShouldBe(expected);
    }

    [Test]
    public void Should_ReportConfiguredOnlyForEnabled_When_UsingNamedOptionStates()
    {
        ApiKeyAuthenticationOptionsMother.Enabled().IsConfigured.ShouldBeTrue();
        ApiKeyAuthenticationOptionsMother.Disabled().IsConfigured.ShouldBeFalse();
        ApiKeyAuthenticationOptionsMother.EnabledWithBlankKey().IsConfigured.ShouldBeFalse();
    }

    [Test]
    public void Should_UseApiKeyAuthenticationSectionName()
    {
        ApiKeyAuthenticationOptions.SectionName.ShouldBe("ApiKeyAuthentication");
    }

    [Test]
    public void Should_NotBeConfigured_When_OptionsAreDefault()
    {
        new ApiKeyAuthenticationOptions().IsConfigured.ShouldBeFalse();
    }

    [TestCase("  secret  ", "secret")]
    [TestCase("secret", "secret")]
    [TestCase("", null)]
    [TestCase("   ", null)]
    [TestCase(null, null)]
    public void Should_TrimValidationKeyAndStoreBlankAsNull_When_Assigned(string? assigned, string? expected)
    {
        var options = new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = assigned };

        options.ValidationKey.ShouldBe(expected);
        options.IsConfigured.ShouldBe(expected is not null);
    }

    [TestCase("  secret  ", "secret")]
    [TestCase("", null)]
    [TestCase("   ", null)]
    public void Should_TrimValidationKey_When_BoundFromConfigurationSection(string bound, string? expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ApiKeyAuthenticationOptions.SectionName}:Enabled"] = "true",
                [$"{ApiKeyAuthenticationOptions.SectionName}:ValidationKey"] = bound
            })
            .Build();

        var options = configuration.GetSection(ApiKeyAuthenticationOptions.SectionName).Get<ApiKeyAuthenticationOptions>();

        options.ShouldNotBeNull();
        options.Enabled.ShouldBeTrue();
        options.ValidationKey.ShouldBe(expected);
    }
}
