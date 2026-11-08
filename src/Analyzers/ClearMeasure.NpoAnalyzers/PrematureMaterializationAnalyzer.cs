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
        ImmutableArray.Create(Descriptors.CM0004);

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

        if (!TryReport(context, continuation))
        {
            return;
        }
    }

    private static bool TryReport(SyntaxNodeAnalysisContext context, MemberAccessExpressionSyntax continuation)
    {
        var continuationName = continuation.Name.Identifier.ValueText;
        if (!Continuations.Contains(continuationName))
        {
            return false;
        }

        if (continuation.Expression is not InvocationExpressionSyntax prior)
        {
            return false;
        }

        return TryReportMaterializer(context, prior, continuationName);
    }

    private static bool TryReportMaterializer(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax prior,
        string continuationName)
    {
        if (prior.Expression is not MemberAccessExpressionSyntax priorAccess)
        {
            return false;
        }

        var materializerName = priorAccess.Name.Identifier.ValueText;
        if (!Materializers.Contains(materializerName))
        {
            return false;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.CM0004,
            priorAccess.Name.GetLocation(),
            materializerName,
            continuationName));
        return true;
    }
}
