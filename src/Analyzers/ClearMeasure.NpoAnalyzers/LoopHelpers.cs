using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.NpoAnalyzers;

internal static class LoopHelpers
{
    private static readonly ImmutableHashSet<string> SelectLikeNames =
        ImmutableHashSet.Create(StringComparer.Ordinal, "Select", "SelectAwait", "SelectMany", "ForEach", "ForEachAsync");

    public static bool IsInsideLoop(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (EntersLoop(current, node))
            {
                return true;
            }
        }

        return false;
    }

    public static string? GetInvocationName(InvocationExpressionSyntax invocation)
    {
        var expression = invocation.Expression;
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.ValueText;
        }

        if (expression is MemberBindingExpressionSyntax memberBinding)
        {
            return memberBinding.Name.Identifier.ValueText;
        }

        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        return GenericName(expression);
    }

    private static string? GenericName(ExpressionSyntax expression)
    {
        if (expression is GenericNameSyntax generic)
        {
            return generic.Identifier.ValueText;
        }

        return null;
    }

    public static InvocationExpressionSyntax? GetAwaitedInvocation(AwaitExpressionSyntax awaitExpr)
    {
        var expression = UnwrapConfigureAwait(SkipParentheses(awaitExpr.Expression));
        return expression as InvocationExpressionSyntax;
    }

    private static bool EntersLoop(SyntaxNode current, SyntaxNode node)
    {
        if (IsLoopBody(current, node))
        {
            return true;
        }

        if (IsRepeatedLoopClause(current, node))
        {
            return true;
        }

        return IsSelectLambda(current);
    }

    private static bool IsLoopBody(SyntaxNode current, SyntaxNode node)
    {
        var body = LoopBody(current);
        return body is not null && Spans(body, node);
    }

    private static StatementSyntax? LoopBody(SyntaxNode current)
    {
        if (current is CommonForEachStatementSyntax forEach)
        {
            return forEach.Statement;
        }

        return OtherLoopBody(current);
    }

    private static StatementSyntax? OtherLoopBody(SyntaxNode current)
    {
        if (current is ForStatementSyntax forStatement)
        {
            return forStatement.Statement;
        }

        if (current is WhileStatementSyntax whileStatement)
        {
            return whileStatement.Statement;
        }

        if (current is DoStatementSyntax doStatement)
        {
            return doStatement.Statement;
        }

        return null;
    }

    private static bool IsRepeatedLoopClause(SyntaxNode current, SyntaxNode node)
    {
        if (IsForClause(current, node))
        {
            return true;
        }

        return IsWhileOrDoClause(current, node);
    }

    private static bool IsForClause(SyntaxNode current, SyntaxNode node) =>
        current is ForStatementSyntax forStatement && RepeatsInFor(forStatement, node);

    private static bool RepeatsInFor(ForStatementSyntax forStatement, SyntaxNode node)
    {
        if (forStatement.Condition is not null && Spans(forStatement.Condition, node))
        {
            return true;
        }

        return IncrementorContains(forStatement, node);
    }

    private static bool IncrementorContains(ForStatementSyntax forStatement, SyntaxNode node)
    {
        foreach (var incrementor in forStatement.Incrementors)
        {
            if (Spans(incrementor, node))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWhileOrDoClause(SyntaxNode current, SyntaxNode node)
    {
        if (current is WhileStatementSyntax whileStatement)
        {
            return Spans(whileStatement.Condition, node);
        }

        if (current is DoStatementSyntax doStatement)
        {
            return Spans(doStatement.Condition, node);
        }

        return false;
    }

    private static bool IsSelectLambda(SyntaxNode current)
    {
        if (current is not (SimpleLambdaExpressionSyntax
            or ParenthesizedLambdaExpressionSyntax
            or AnonymousMethodExpressionSyntax))
        {
            return false;
        }

        return IsSelectArgument(current.Parent);
    }

    private static bool IsSelectArgument(SyntaxNode? parent)
    {
        if (parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } })
        {
            return false;
        }

        string? name = GetInvocationName(invocation);
        return name is not null && SelectLikeNames.Contains(name);
    }

    private static ExpressionSyntax UnwrapConfigureAwait(ExpressionSyntax expression)
    {
        expression = SkipParentheses(expression);
        if (expression is not InvocationExpressionSyntax invocation || !IsConfigureAwait(invocation))
        {
            return expression;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return expression;
        }

        return UnwrapConfigureAwait(memberAccess.Expression);
    }

    private static bool IsConfigureAwait(InvocationExpressionSyntax invocation) =>
        GetInvocationName(invocation) == "ConfigureAwait";

    private static ExpressionSyntax SkipParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    private static bool Spans(SyntaxNode ancestor, SyntaxNode node) =>
        ancestor.Span.Contains(node.Span);
}
