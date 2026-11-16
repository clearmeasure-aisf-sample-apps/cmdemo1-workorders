using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;
public class DeploymentIdentityTests
{
    [Test]
    public void ShortSha_ShouldReturnFirstSevenCharacters_WhenGitShaIsKnown()
    {
        var identity = new DeploymentIdentity("0123456789abcdef", "tdd");

        identity.ShortSha.ShouldBe("0123456");
    }

    [Test]
    public void ShortSha_ShouldBeNull_WhenGitShaIsUnknown()
    {
        DeploymentIdentity.Unreported.ShortSha.ShouldBeNull();
    }

    [Test]
    public void FromProcess_ShouldReturnUnknownEnvironment_WhenAspNetAndDotNetEnvAreUnset()
    {
        var previousAspNet = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var previousDotNet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            var identity = DeploymentIdentity.FromProcess();

            identity.EnvironmentName.ShouldBe("unknown");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previousAspNet);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", previousDotNet);
        }
    }
}

