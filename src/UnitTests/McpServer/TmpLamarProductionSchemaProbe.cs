using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Tools;
using FluentValidation;
using Lamar;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

// TEMPORARY PROBE - verifies the production Lamar path; delete after verification.
[TestFixture]
public class TmpLamarProductionSchemaProbe
{
    [Test]
    public void CreateTeamSchema_UnderLamar_Still_ExposesMemberUserNames()
    {
        var registry = new ServiceRegistry();
        registry.IncludeRegistry<McpServiceRegistry>();
        // Mirror what Lamar.Microsoft.DependencyInjection.UseLamar registers for the host.
        registry.AddSingleton(sp => (IServiceProviderIsService)sp.GetRequiredService<IContainer>());

        registry
            .AddMcpServer(options =>
            {
                options.ServerInfo = new()
                {
                    Name = "ChurchBulletin",
                    Version = "1.0.0"
                };
            })
            .WithTools<TeamTools>();

        using var container = new Container(registry);
        var isService = container.GetRequiredService<IServiceProviderIsService>();
        isService.ShouldNotBeNull();
        isService.IsService(typeof(string[])).ShouldBeFalse(
            "string[] must remain a tool argument, not an injectable service");

        var tool = container.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "create-team");
        tool.ProtocolTool.InputSchema.GetProperty("properties").TryGetProperty("memberUserNames", out _).ShouldBeTrue();
    }

    [Test]
    public void Container_Still_ResolvesCoreAppServices()
    {
        var registry = new ServiceRegistry();
        registry.IncludeRegistry<McpServiceRegistry>();
        using var container = new Container(registry);

        container.GetRequiredService<IBus>().ShouldNotBeNull();
        container.GetRequiredService<IMediator>().ShouldNotBeNull();
        container.GetRequiredService<CreateTeamCommandShapeRules>().ShouldNotBeNull();
        container.GetRequiredService<CreateVendorCommandShapeRules>().ShouldNotBeNull();
        container.GetRequiredService<CreateDatedWorkOrdersCommandShapeRules>().ShouldNotBeNull();
        container.GetRequiredService<IValidator<SaveDraftCommand>>().ShouldNotBeNull();
        container.TryGetInstance<IValidator<CreateTeamCommand>>().ShouldBeNull();
    }
}
