using ArchUnitNET.Loader;
using ChurchBulletin.ServiceDefaults;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.Database.Console;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using Worker;
using ArchArchitecture = ArchUnitNET.Domain.Architecture;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// The compiled solution as ArchUnitNET sees it, and the Onion layering it must keep.
/// Core is the center; DataAccess and LlmGateway depend on Core only; UI.Server is the composition root
/// and no other layer depends on it. Changing <see cref="Layers"/> changes the architecture: it needs a
/// reviewed exception (docs/architecture-guardrails.md).
/// </summary>
public static class SolutionArchitecture
{
    private const string Core = "Core";
    private const string DataAccess = "DataAccess";
    private const string LlmGateway = "LlmGateway";
    private const string UiShared = "UI.Shared";
    private const string ServiceDefaults = "ServiceDefaults";

    private static readonly Lazy<ArchArchitecture> Loaded = new(Load);

    /// <summary>Every production layer of the solution, innermost first.</summary>
    public static IReadOnlyList<OnionLayer> Layers { get; } =
    [
        new(Core, "ClearMeasure.Bootcamp.Core", []),
        new(DataAccess, "ClearMeasure.Bootcamp.DataAccess", [Core]),
        new(LlmGateway, "ClearMeasure.Bootcamp.LlmGateway", [Core]),
        new("Database", "ClearMeasure.Bootcamp.Database", []),
        new(ServiceDefaults, "ChurchBulletin.ServiceDefaults", []),
        new(UiShared, "ClearMeasure.Bootcamp.UI.Shared", [Core, LlmGateway]),
        new("UI.Client", "ClearMeasure.Bootcamp.UI.Client", [Core, UiShared, LlmGateway]),
        new("UI.Api", "ClearMeasure.Bootcamp.UI.Api", [Core]),
        new("McpServer", "ClearMeasure.Bootcamp.McpServer", [Core, DataAccess, LlmGateway, UiShared, ServiceDefaults]),
        new("Worker", "Worker", [Core, DataAccess, LlmGateway, ServiceDefaults]),
    ];

    /// <summary>
    /// Layers whose namespaces already had dependency cycles when the cycle rule was introduced: Core
    /// (Model, Queries, Services), UI.Shared (Pages, Components, Models, Services) and McpServer
    /// (Tools, Tools.Arguments). The list may only shrink.
    /// </summary>
    public static IReadOnlySet<string> LayersWithKnownNamespaceCycles { get; } =
        new HashSet<string>(StringComparer.Ordinal) { Core, UiShared, "McpServer" };

    /// <summary>The composition root: it may depend on every layer, and no layer may depend on it.</summary>
    public static OnionLayer CompositionRoot { get; } = new("UI.Server", "ClearMeasure.Bootcamp.UI.Server", []);

    /// <summary>Regular expression matching the namespace of every solution layer.</summary>
    public static string SolutionNamespacePattern =>
        string.Join("|", Layers.Append(CompositionRoot).Select(layer => layer.NamespacePattern));

    /// <summary>The production assemblies, loaded once per test run.</summary>
    public static ArchArchitecture Current => Loaded.Value;

    /// <summary>Layers that <paramref name="layer"/> must not depend on.</summary>
    public static IEnumerable<OnionLayer> ForbiddenFor(OnionLayer layer)
    {
        return Layers
            .Append(CompositionRoot)
            .Where(other => other.Name != layer.Name && !layer.MayDependOn.Contains(other.Name));
    }

    private static ArchArchitecture Load()
    {
        return new ArchLoader()
            .LoadAssemblies(
                typeof(WorkOrder).Assembly,
                typeof(DatabaseMigrationVersionReader).Assembly,
                typeof(TranslationService).Assembly,
                typeof(NullUpgradeLog).Assembly,
                typeof(LogEntry).Assembly,
                typeof(SearchPageScript).Assembly,
                typeof(RemotableBus).Assembly,
                typeof(ApiRoutes).Assembly,
                typeof(McpServiceRegistry).Assembly,
                typeof(WorkOrderEndpoint).Assembly,
                typeof(DetailedHealthReportProvider).Assembly)
            .Build();
    }
}
