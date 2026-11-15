using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware;

[TestFixture]
public class HttpEnvelopeValidatorSetTests
{
    [Test]
    public void For_ShouldMapDeferredPayloads_AndIgnoreOthers()
    {
        var set = new HttpEnvelopeValidatorSet(new ServiceCollection().BuildServiceProvider());

        set.For(new CreateVendorCommand("", null, null, null, null))
            .ShouldBeOfType<CreateVendorCommandValidator>();
        set.For(new CreateTeamCommand("Crew", null, []))
            .ShouldBeOfType<CreateTeamCommandValidator>();
        set.For(new CreateDatedWorkOrdersCommand("a", "b", "t", "d", []))
            .ShouldBeOfType<CreateDatedWorkOrdersCommandValidator>();
        set.For(new object()).ShouldBeNull();
    }
}
