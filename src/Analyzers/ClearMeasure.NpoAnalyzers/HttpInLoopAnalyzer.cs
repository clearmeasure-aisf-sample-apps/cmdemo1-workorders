using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.NpoAnalyzers;

/// <summary>
/// Reports CM0003 for HTTP calls inside loops. Disabled in .editorconfig until #1520.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HttpInLoopAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> HttpMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "GetAsync", "PostAsync", "PutAsync", "DeleteAsync", "PatchAsync", "SendAsync",
        "GetStringAsync", "GetByteArrayAsync", "GetStreamAsync", "GetFromJsonAsync",
        "PostAsJsonAsync", "PutAsJsonAsync");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CM0003);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAwait, SyntaxKind.AwaitExpression);
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

        var name = LoopHelpers.GetInvocationName(invocation);
        if (name is null || !HttpMethods.Contains(name))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.CM0003, invocation.GetLocation(), name));
    }
}
