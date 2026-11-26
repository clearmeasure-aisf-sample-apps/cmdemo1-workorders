using ClearMeasure.Bootcamp.UI.Server;
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
}
