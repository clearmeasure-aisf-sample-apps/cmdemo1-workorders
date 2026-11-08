using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.NpoAnalyzers;

/// <summary>
/// Reports CM0002 when an EF materializer inside a loop has an <c>IQueryable</c> receiver.
/// In-memory <c>ToArray</c> / <c>FirstOrDefault</c> calls are ignored. Severity is warning.
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
        [Descriptors.Cm0002];

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
        if (name is null || !Materializers.Contains(name) || !IsQueryableReceiver(context, invocation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.Cm0002, invocation.GetLocation(), name));
    }

    private static bool IsQueryableReceiver(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var type = ReceiverType(context, invocation);
        return type is not null && IsQueryable(type);
    }

    private static ITypeSymbol? ReceiverType(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var receiver = Receiver(invocation);
        if (receiver is null)
        {
            return null;
        }

        return context.SemanticModel.GetTypeInfo(receiver).Type;
    }

    private static ExpressionSyntax? Receiver(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax access)
        {
            return access.Expression;
        }

        if (invocation.Parent is ConditionalAccessExpressionSyntax conditional)
        {
            return conditional.Expression;
        }

        return null;
    }

    private static bool IsQueryable(ITypeSymbol type)
    {
        if (IsQueryableType(type))
        {
            return true;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (IsQueryableType(iface))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsQueryableType(ITypeSymbol type) =>
        type is INamedTypeSymbol
        {
            Name: "IQueryable",
            ContainingNamespace:
            {
                Name: "Linq",
                ContainingNamespace:
                {
                    Name: "System",
                    ContainingNamespace: { IsGlobalNamespace: true }
                }
            }
        };
}
