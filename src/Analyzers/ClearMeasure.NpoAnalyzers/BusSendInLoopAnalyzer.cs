using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.NpoAnalyzers;

/// <summary>
/// Reports CM0001 when an awaited bus or mediator send runs inside a loop or a Select lambda.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global -- loaded by Roslyn reflection, not new()
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BusSendInLoopAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> SendNames =
        ImmutableHashSet.Create(StringComparer.Ordinal, "Send", "SendAsync", "Publish", "PublishAsync");

    private static readonly ImmutableHashSet<string> NamedReceivers =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Bus",
            "bus",
            "_bus",
            "Mediator",
            "_mediator",
            "mediator",
            "Sender",
            "_sender",
            "sender");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CM0001);

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
        if (invocation is null || !IsBusSend(context, invocation))
        {
            return;
        }

        var name = LoopHelpers.GetInvocationName(invocation) ?? "Send";
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.CM0001, invocation.GetLocation(), name));
    }

    private static bool IsBusSend(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var name = LoopHelpers.GetInvocationName(invocation);
        if (name is null || !SendNames.Contains(name))
        {
            return false;
        }

        return IsBusTarget(context, invocation);
    }

    private static bool IsBusTarget(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        if (IsNamedBusReceiver(invocation))
        {
            return true;
        }

        return ResolvesToBus(context.SemanticModel.GetSymbolInfo(invocation));
    }

    private static bool ResolvesToBus(SymbolInfo info)
    {
        if (IsBusSymbol(info.Symbol))
        {
            return true;
        }

        return AnyBusCandidate(info.CandidateSymbols);
    }

    private static bool IsBusSymbol(ISymbol? symbol) =>
        symbol is IMethodSymbol method && IsBusLikeMethod(method);

    private static bool AnyBusCandidate(ImmutableArray<ISymbol> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (IsBusSymbol(candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBusLikeMethod(IMethodSymbol method)
    {
        var type = method.ContainingType;
        if (type is null)
        {
            return false;
        }

        return IsBusType(type) || ImplementsBus(type);
    }

    private static bool ImplementsBus(INamedTypeSymbol type)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (IsBusType(iface))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBusType(INamedTypeSymbol type) =>
        type.Name is "IBus" or "IMediator" or "ISender";

    private static bool IsNamedBusReceiver(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax access)
        {
            return false;
        }

        var receiver = access.Expression.ToString();
        if (NamedReceivers.Contains(receiver))
        {
            return true;
        }

        return receiver.EndsWith(".Bus", StringComparison.Ordinal)
            || receiver.EndsWith(".Mediator", StringComparison.Ordinal);
    }
}
