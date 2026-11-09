using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ClearMeasure.NpoAnalyzers;

/// <summary>
/// Reports CM0001 when an awaited bus or mediator send runs inside a loop or a Select lambda,
/// including one level of a local function or same-type helper that awaits that send.
/// </summary>
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
        [Descriptors.Cm0001];

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

        if (IsBusSend(context.SemanticModel, invocation) || FollowsBusHelper(context, invocation))
        {
            Report(context, invocation);
        }
    }

    private static void Report(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        string name = LoopHelpers.GetInvocationName(invocation) ?? "Send";
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.Cm0001, invocation.GetLocation(), name));
    }

    private static bool FollowsBusHelper(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        var method = SameTypeHelper(context, invocation);
        return method is not null && HelperAwaitsBus(context, method);
    }

    private static IMethodSymbol? SameTypeHelper(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation)
    {
        if (context.SemanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method || !CanFollow(context, method))
        {
            return null;
        }

        return method.DeclaringSyntaxReferences.Length == 0 ? null : method;
    }

    private static bool CanFollow(SyntaxNodeAnalysisContext context, IMethodSymbol method)
    {
        if (method.MethodKind == MethodKind.LocalFunction)
        {
            return true;
        }

        var caller = context.ContainingSymbol?.ContainingType;
        return caller is not null && SymbolEqualityComparer.Default.Equals(method.ContainingType, caller);
    }

    private static bool HelperAwaitsBus(SyntaxNodeAnalysisContext context, IMethodSymbol method)
    {
        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (ReferenceAwaitsBus(context, reference))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ReferenceAwaitsBus(SyntaxNodeAnalysisContext context, SyntaxReference reference)
    {
        var syntax = reference.GetSyntax(context.CancellationToken);
        if (syntax.SyntaxTree != context.Node.SyntaxTree)
        {
            return false;
        }

        var body = MethodBody(syntax);
        return body is not null && ContainsBusAwait(context.SemanticModel, body);
    }

    private static SyntaxNode? MethodBody(SyntaxNode syntax) =>
        syntax switch
        {
            MethodDeclarationSyntax method => method.Body ?? (SyntaxNode?)method.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax local => local.Body ?? (SyntaxNode?)local.ExpressionBody?.Expression,
            _ => null
        };

    private static bool ContainsBusAwait(SemanticModel model, SyntaxNode body)
    {
        foreach (var node in body.DescendantNodesAndSelf(DescendInto))
        {
            if (node is AwaitExpressionSyntax awaitExpr && IsBusAwait(model, awaitExpr))
            {
                return true;
            }
        }

        return false;
    }

    private static bool DescendInto(SyntaxNode node) => node is not LocalFunctionStatementSyntax;

    private static bool IsBusAwait(SemanticModel model, AwaitExpressionSyntax awaitExpr)
    {
        var invocation = LoopHelpers.GetAwaitedInvocation(awaitExpr);
        return invocation is not null && IsBusSend(model, invocation);
    }

    private static bool IsBusSend(SemanticModel model, InvocationExpressionSyntax invocation)
    {
        string? name = LoopHelpers.GetInvocationName(invocation);
        if (name is null || !SendNames.Contains(name))
        {
            return false;
        }

        return IsBusTarget(model, invocation);
    }

    private static bool IsBusTarget(SemanticModel model, InvocationExpressionSyntax invocation)
    {
        if (IsNamedBusReceiver(invocation))
        {
            return true;
        }

        return ResolvesToBus(model.GetSymbolInfo(invocation));
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

        string receiver = access.Expression.ToString();
        if (NamedReceivers.Contains(receiver))
        {
            return true;
        }

        return receiver.EndsWith(".Bus", StringComparison.Ordinal)
            || receiver.EndsWith(".Mediator", StringComparison.Ordinal);
    }
}
