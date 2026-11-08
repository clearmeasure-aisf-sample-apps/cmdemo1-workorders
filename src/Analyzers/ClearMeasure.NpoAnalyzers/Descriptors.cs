using Microsoft.CodeAnalysis;

namespace ClearMeasure.NpoAnalyzers;

internal static class Descriptors
{
    public static readonly DiagnosticDescriptor Cm0001 = new(
        id: "CM0001",
        title: "Await of bus/mediator Send inside a loop",
        messageFormat: "Await of '{0}' inside a loop scales round-trips with the collection size; load/aggregate with one Send instead",
        category: "Performance.DataAccess",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Detects an awaited Send, SendAsync, or Publish on IBus, IMediator, or ISender, and on a receiver named Bus, bus, or _bus, inside foreach, for, while, do, or a LINQ Select lambda. Also flags one level of a local function or same-type helper whose body awaits that send.");

    public static readonly DiagnosticDescriptor Cm0002 = new(
        id: "CM0002",
        title: "EF Core materializer or DbContext query inside a loop",
        messageFormat: "EF Core call '{0}' inside a loop is an N+1 risk; query once outside the loop or use a set-based filter (Contains/GroupBy)",
        category: "Performance.DataAccess",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Detects ToListAsync, FirstOrDefaultAsync, and similar materializers on an IQueryable inside a loop. In-memory LINQ is ignored.");

    public static readonly DiagnosticDescriptor Cm0003 = new(
        id: "CM0003",
        title: "HttpClient or remoted API call inside a loop",
        messageFormat: "HTTP call '{0}' inside a loop is a chatty UI/API pattern; batch or use one aggregate request",
        category: "Performance.DataAccess",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "Shipped disabled (#1520).");

    public static readonly DiagnosticDescriptor Cm0004 = new(
        id: "CM0004",
        title: "ToList/AsEnumerable before Where/GroupBy on a queryable",
        messageFormat: "'{0}' materializes (or switches to LINQ-to-Objects) before '{1}'; keep filters on IQueryable so they run in SQL",
        category: "Performance.DataAccess",
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true,
        description: "Shipped disabled (#1520).");

    public static readonly DiagnosticDescriptor Cm0005 = new(
        id: "CM0005",
        title: "DbContext referenced from a UI project",
        messageFormat: "Type '{0}' looks like a DbContext and is referenced from a UI assembly; keep persistence behind IBus/handlers",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Detects DbContext-derived types on fields, properties, parameters, and return types in UI.* assemblies.");
}
