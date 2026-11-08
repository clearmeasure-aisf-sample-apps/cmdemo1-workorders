using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.NpoAnalyzers;

/// <summary>
/// Reports CM0005 when a UI assembly declares a member whose type is a DbContext.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DbContextInUiAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] UiNameParts = { "UI.Shared", "UI.Client", "UI.Server", "UI.Api" };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CM0005);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (!IsUiAssembly(type.ContainingAssembly?.Name ?? string.Empty))
        {
            return;
        }

        foreach (var member in type.GetMembers())
        {
            ReportMember(context, type, member);
        }
    }

    private static void ReportMember(SymbolAnalysisContext context, INamedTypeSymbol owner, ISymbol member)
    {
        if (member is IFieldSymbol field)
        {
            ReportTypeTree(context, field.Type, LocationOf(field, owner));
            return;
        }

        if (member is IPropertySymbol property)
        {
            ReportTypeTree(context, property.Type, LocationOf(property, owner));
            return;
        }

        if (member is IMethodSymbol method)
        {
            ReportMethod(context, owner, method);
        }
    }

    private static void ReportMethod(SymbolAnalysisContext context, INamedTypeSymbol owner, IMethodSymbol method)
    {
        foreach (var parameter in method.Parameters)
        {
            ReportTypeTree(context, parameter.Type, LocationOf(parameter, owner));
        }

        if (method.ReturnsVoid)
        {
            return;
        }

        ReportTypeTree(context, method.ReturnType, LocationOf(method, owner));
    }

    private static bool IsUiAssembly(string name)
    {
        if (name.EndsWith(".UI", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var part in UiNameParts)
        {
            if (name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void ReportTypeTree(SymbolAnalysisContext context, ITypeSymbol type, Location location)
    {
        if (IsDbContextType(type))
        {
            Report(context, location, type.Name);
            return;
        }

        ReportGenericArguments(context, type, location);
    }

    private static void ReportGenericArguments(SymbolAnalysisContext context, ITypeSymbol type, Location location)
    {
        if (type is not INamedTypeSymbol named)
        {
            return;
        }

        foreach (var argument in named.TypeArguments)
        {
            ReportArgument(context, location, argument);
        }
    }

    private static void ReportArgument(SymbolAnalysisContext context, Location location, ITypeSymbol argument)
    {
        if (IsDbContextType(argument))
        {
            Report(context, location, argument.Name);
        }
    }

    private static void Report(SymbolAnalysisContext context, Location location, string name) =>
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.CM0005, location, name));

    private static bool IsDbContextType(ITypeSymbol type)
    {
        if (HasText(type.Name, "DbContext") || type.Name == "DataContext")
        {
            return true;
        }

        if (HasText(type.ToDisplayString(), "EntityFrameworkCore.DbContext"))
        {
            return true;
        }

        return InheritsDbContext(type);
    }

    private static bool InheritsDbContext(ITypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "DbContext")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasText(string text, string value) =>
        text.IndexOf(value, StringComparison.Ordinal) >= 0;

    private static Location LocationOf(ISymbol symbol, INamedTypeSymbol fallback)
    {
        var location = symbol.Locations.FirstOrDefault();
        if (location is not null)
        {
            return location;
        }

        return fallback.Locations.First();
    }
}
