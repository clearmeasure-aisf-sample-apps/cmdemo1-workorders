using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.NpoAnalyzers;

/// <summary>
/// Reports CM0002 for EF materializers inside loops. Disabled in .editorconfig until #1520.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EfMaterializerInLoopAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> Materializers = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ToListAsync", "ToArrayAsync", "ToDictionaryAsync", "ToHashSetAsync",
        "FirstAsync", "FirstOrDefaultAsync", "SingleAsync", "SingleOrDefaultAsync",
        "LastAsync", "LastOrDefaultAsync", "CountAsync", "LongCountAsync", "AnyAsync", "AllAsync",
        "SumAsync", "AverageAsync", "MinAsync", "MaxAsync",
        "FindAsync", "Find", "LoadAsync", "Load",
        "ExecuteDeleteAsync", "ExecuteUpdateAsync",
        "ToList", "ToArray", "First", "FirstOrDefault", "Single", "SingleOrDefault", "Count");

    private static readonly ImmutableHashSet<string> SyncMaterializers = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Find", "Load", "ToList", "ToArray", "First", "FirstOrDefault", "Single", "SingleOrDefault", "Count");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CM0002);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAwait, SyntaxKind.AwaitExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeAwait(SyntaxNodeAnalysisContext context)
    {
        var awaitExpr = (AwaitExpressionSyntax)context.Node;
        if (!LoopHelpers.IsInsideLoop(awaitExpr))
        {
            return;
        }

        var invocation = LoopHelpers.GetAwaitedInvocation(awaitExpr);
        if (invocation is null)
        {
            return;
        }

        ReportIfMaterializer(context, invocation);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Parent is AwaitExpressionSyntax || !LoopHelpers.IsInsideLoop(invocation))
        {
            return;
        }

        var name = LoopHelpers.GetInvocationName(invocation);
        if (name is null || !SyncMaterializers.Contains(name))
        {
            return;
        }

        ReportIfMaterializer(context, invocation);
    }

    private static void ReportIfMaterializer(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var name = LoopHelpers.GetInvocationName(invocation);
        if (name is null || !Materializers.Contains(name))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.CM0002, invocation.GetLocation(), name));
    }
}
