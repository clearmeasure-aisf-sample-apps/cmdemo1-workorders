using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers;

internal static class AnalyzerTestHelper
{
    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        string assemblyName = "AnalyzerTest")
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [tree],
            FrameworkReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        if (errors.Length > 0)
        {
            string message = string.Join(Environment.NewLine, errors.Select(error => error.ToString()));
            throw new InvalidOperationException(message);
        }

        var withAnalyzers = compilation.WithAnalyzers([analyzer]);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static MetadataReference[] FrameworkReferences()
    {
        var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Add(locations, typeof(object).Assembly);
        Add(locations, typeof(Enumerable).Assembly);
        Add(locations, typeof(IQueryable).Assembly);
        Add(locations, typeof(System.Linq.Expressions.Expression).Assembly);
        Add(locations, typeof(Task).Assembly);
        Add(locations, typeof(List<>).Assembly);
        Add(locations, typeof(HttpClient).Assembly);
        Add(locations, typeof(Uri).Assembly);
        Add(locations, typeof(Attribute).Assembly);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? name = assembly.GetName().Name;
            if (name is "System.Runtime" or "netstandard" or "System.Collections" or "System.Linq")
            {
                Add(locations, assembly);
            }
        }

        return locations.Select(MetadataReference (location) => MetadataReference.CreateFromFile(location)).ToArray();
    }

    private static void Add(HashSet<string> locations, Assembly assembly)
    {
        if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
        {
            locations.Add(assembly.Location);
        }
    }
}
