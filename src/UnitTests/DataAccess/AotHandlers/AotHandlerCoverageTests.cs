using System.Reflection;
using System.Xml.Linq;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server.Handlers;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary>
/// Every Mediator handler in the UI.Server Native AOT build either is an Aot* snapshot handler with a JIT/AOT parity
/// fixture, or is listed in <see cref="AotSafeHandlerRegister"/> with a reason. SlaHealthQueryHandler was neither and
/// answered HTTP 500 under AOT (PR #1911); a new handler fails here until it is classified.
/// </summary>
[TestFixture]
public class AotHandlerCoverageTests
{
    private const string AotHandlerNamespace = "ClearMeasure.Bootcamp.DataAccess.Handlers";
    private const string ParityFixtureNamespace = "ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers";

    [Test]
    public void EveryHandlerInTheAotBuild_Should_BeRegisteredAsAotSafe_When_ItIsNotAnAotHandler()
    {
        var unclassified = HandlersInTheAotBuild()
            .Select(handler => handler.Name)
            .Where(name => !AotSafeHandlerRegister.Handlers.ContainsKey(name))
            .Order()
            .ToList();

        unclassified.ShouldBeEmpty(
            "Add an Aot* handler and parity fixture, or register the handler with why it is safe under Native AOT.");
    }

    [Test]
    public void AotSafeHandlerRegister_Should_ListOnlyHandlersInTheAotBuild_When_Read()
    {
        var inTheBuild = HandlersInTheAotBuild().Select(handler => handler.Name).ToHashSet();

        AotSafeHandlerRegister.Handlers.Keys.Where(name => !inTheBuild.Contains(name)).ShouldBeEmpty();
        AotSafeHandlerRegister.Handlers.Values.ShouldAllBe(reason => !string.IsNullOrWhiteSpace(reason));
    }

    [Test]
    public void EveryHandlerRemovedFromTheAotBuild_Should_HaveAnAotHandler_When_ItHandlesARequest()
    {
        var aotRequests = AotHandlers().SelectMany(RequestTypes).ToHashSet();

        var uncovered = JitHandlersRemovedFromTheAotBuild()
            .SelectMany(handler => RequestTypes(handler).Select(request => $"{handler.Name} -> {request.Name}"))
            .Where(entry => !aotRequests.Any(request => entry.EndsWith($"-> {request.Name}", StringComparison.Ordinal)))
            .ToList();

        uncovered.ShouldBeEmpty();
    }

    [Test]
    public void EveryAotHandler_Should_HaveAParityFixture_When_ItReplacesAJitHandler()
    {
        var withoutFixture = AotHandlers()
            .Select(handler => handler.Name)
            .Where(name => typeof(AotHandlerCoverageTests).Assembly.GetType($"{ParityFixtureNamespace}.{name}Tests") is null)
            .ToList();

        withoutFixture.ShouldBeEmpty();
    }

    /// <summary>The handlers UI.Server's AOT build compiles, Aot* handlers aside.</summary>
    private static IEnumerable<Type> HandlersInTheAotBuild()
    {
        var removed = RemovedUnderNativeAotDataAccess();
        var dataAccess = HandlersIn(typeof(DataContext).Assembly).Where(handler => !removed.Contains(handler.Name));
        var server = HandlersIn(typeof(ServerHealthCheckHandler).Assembly);
        var llmGateway = HandlersIn(typeof(ApplicationChatHandler).Assembly);
        return dataAccess.Concat(server).Concat(llmGateway).Distinct();
    }

    private static IEnumerable<Type> JitHandlersRemovedFromTheAotBuild()
    {
        var removed = RemovedUnderNativeAotDataAccess();
        return HandlersIn(typeof(DataContext).Assembly).Where(handler => removed.Contains(handler.Name));
    }

    /// <summary>The Aot* handlers, which compile only in the AOT build and are linked into this test project.</summary>
    private static IEnumerable<Type> AotHandlers() =>
        HandlersIn(typeof(AotSlaHealthQueryHandler).Assembly)
            .Where(handler => handler.Namespace == AotHandlerNamespace && handler.Name.StartsWith("Aot", StringComparison.Ordinal));

    /// <summary>File names DataAccess.csproj compiles out when NativeAotDataAccess is true.</summary>
    private static HashSet<string> RemovedUnderNativeAotDataAccess()
    {
        var project = XDocument.Parse(RepositoryCheckout.Current.ReadText("src/DataAccess/DataAccess.csproj"));
        return project.Descendants("ItemGroup")
            .Where(group => (string?)group.Attribute("Condition") == "'$(NativeAotDataAccess)' == 'true'")
            .Elements("Compile")
            .Select(item => Path.GetFileNameWithoutExtension(((string)item.Attribute("Remove")!).Replace('\\', '/')))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<Type> HandlersIn(Assembly assembly) =>
        assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false } && RequestTypes(type).Any());

    private static IEnumerable<Type> RequestTypes(Type handler) =>
        handler.GetInterfaces()
            .Where(contract => contract is { IsGenericType: true, Namespace: "Mediator" }
                && contract.Name.Contains("Handler", StringComparison.Ordinal))
            .Select(contract => contract.GetGenericArguments()[0]);
}
