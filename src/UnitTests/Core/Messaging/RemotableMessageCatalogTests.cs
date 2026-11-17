using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Messaging;

/// <summary>The catalog finds a message's source-generated contract by type or by the type name a remoted body carries.</summary>
[TestFixture]
public class RemotableMessageCatalogTests
{
    [Test]
    public void ContractFor_Should_FindTheSameContract_ByTypeAndByItsTypeName()
    {
        var typeName = RemotableMessageCatalog.TypeNameOf(typeof(ServerHealthCheckQuery));

        RemotableMessageCatalog.ContractFor(typeName).ShouldBeSameAs(RemotableMessageCatalog.ContractFor(typeof(ServerHealthCheckQuery)));
        typeName.ShouldBe("ClearMeasure.Bootcamp.Core.Queries.ServerHealthCheckQuery, ClearMeasure.Bootcamp.Core");
    }

    [Test]
    public void ContractFor_Should_RefuseATypeThatIsNotRemotable()
    {
        Should.Throw<InvalidOperationException>(() => RemotableMessageCatalog.ContractFor("System.IO.File, System.Runtime"))
            .Message.ShouldBe("System.IO.File, System.Runtime is not a remotable message type.");
    }

    [Test]
    public void ValidatorFor_Should_ReturnNull_When_NoValidatorIsRegistered()
    {
        using var services = new ServiceCollection().BuildServiceProvider();

        RemotableMessageCatalog.ValidatorFor(new ServerHealthCheckQuery(), services).ShouldBeNull();
    }
}
