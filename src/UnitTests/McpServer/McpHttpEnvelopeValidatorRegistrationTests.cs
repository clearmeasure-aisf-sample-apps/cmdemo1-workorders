using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpHttpEnvelopeValidatorRegistrationTests
{
    [Test]
    public void ShouldOmitDeferredIValidators_WhenMcpServerServicesAreAdded()
    {
        using var provider = BuildProvider();

        provider.GetService<IValidator<CreateVendorCommand>>().ShouldBeNull();
        provider.GetService<IValidator<CreateTeamCommand>>().ShouldBeNull();
        provider.GetService<IValidator<CreateDatedWorkOrdersCommand>>().ShouldBeNull();
        provider.GetRequiredService<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddMcpServerServices();
        return services.BuildServiceProvider();
    }
}
