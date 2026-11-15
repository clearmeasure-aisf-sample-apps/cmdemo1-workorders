using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class DeploymentIdentityQueryHandlerTests
{
    [Test]
    public async Task Handle_ShouldReturnProcessIdentity()
    {
        var handler = new DeploymentIdentityQueryHandler();

        var result = await handler.Handle(new DeploymentIdentityQuery(), CancellationToken.None);

        result.ShouldBe(DeploymentIdentity.FromProcess());
    }
}
