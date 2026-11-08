using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.NpoAnalyzers;

/// <summary>
/// Reports CM0004 when a materializer runs before a later query operator. Disabled until #1520.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrematureMaterializationAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> Materializers =
        ImmutableHashSet.Create(StringComparer.Ordinal, "ToList", "ToArray", "AsEnumerable", "ToHashSet");

    private static readonly ImmutableHashSet<string> Continuations =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Where", "GroupBy", "OrderBy", "OrderByDescending",
            "Select", "SelectMany", "Skip", "Take", "Count", "Any", "All", "First", "FirstOrDefault",
            "Single", "SingleOrDefault", "Sum", "Average", "Min", "Max");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Descriptors.Cm0004];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax continuation)
        {
            return;
        }

        ReportContinuation(context, continuation);
    }

    private static void ReportContinuation(SyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax continuation)
    {
        var continuationName = continuation.Name.Identifier.ValueText;
        if (!Continuations.Contains(continuationName))
        {
            return;
        }

        if (continuation.Expression is not InvocationExpressionSyntax prior)
        {
            return;
        }

        ReportMaterializer(context, prior, continuationName);
    }

    private static void ReportMaterializer(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax prior,
        string continuationName)
    {
        if (prior.Expression is not MemberAccessExpressionSyntax priorAccess)
        {
            return;
        }

        var materializerName = priorAccess.Name.Identifier.ValueText;
        if (!Materializers.Contains(materializerName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.Cm0004,
            priorAccess.Name.GetLocation(),
            materializerName,
            continuationName));
    }
}
