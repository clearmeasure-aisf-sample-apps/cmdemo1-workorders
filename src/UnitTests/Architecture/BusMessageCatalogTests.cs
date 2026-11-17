using System.Reflection;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Every Mediator message type must implement <see cref="IBusMessage"/>. The marker carries the
/// trimming annotation that keeps message properties for bus activity tags under Native AOT.
/// </summary>
[TestFixture]
public class BusMessageCatalogTests
{
    private static readonly Assembly[] ProductionAssemblies =
    [
        typeof(IBusMessage).Assembly,
        typeof(global::ClearMeasure.Bootcamp.LlmGateway.ChatClientConfigQuery).Assembly,
        typeof(Bus).Assembly,
        typeof(RemotableBus).Assembly,
        typeof(global::ClearMeasure.Bootcamp.DataAccess.Mappings.DataContext).Assembly,
        typeof(global::ClearMeasure.Bootcamp.UI.Server.DatabaseConfiguration).Assembly,
        typeof(global::ClearMeasure.Bootcamp.McpServer.McpServerApplication).Assembly,
    ];

    [Test]
    public void ShouldMarkEveryMessageAsBusMessage_WhenProductionAssembliesAreScanned()
    {
        var unmarked = MessageTypes()
            .Where(type => !typeof(IBusMessage).IsAssignableFrom(type))
            .Select(type => type.FullName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        unmarked.ShouldBeEmpty(
            "Message types that do not implement IBusMessage: " + string.Join(", ", unmarked));
    }

    [Test]
    public void ShouldFindMessagesInEveryLayer_WhenProductionAssembliesAreScanned()
    {
        var assemblies = MessageTypes().Select(type => type.Assembly).Distinct().ToArray();

        assemblies.ShouldContain(typeof(IBusMessage).Assembly);
        assemblies.ShouldContain(typeof(global::ClearMeasure.Bootcamp.LlmGateway.ChatClientConfigQuery).Assembly);
    }

    private static IEnumerable<Type> MessageTypes() =>
        ProductionAssemblies
            .Distinct()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false } || type is { IsValueType: true })
            .Where(type => typeof(Mediator.IMessage).IsAssignableFrom(type));
}