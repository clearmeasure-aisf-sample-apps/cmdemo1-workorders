using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Decides whether a method or accessor body is only a forward to one member.
/// </summary>
internal static class PassThroughExpressions
{
    public static bool TryMemberTarget(
        ArrowExpressionClauseSyntax? arrow,
        BlockSyntax? block,
        HashSet<string> members,
        out string? target)
    {
        if (arrow != null)
        {
            return TryExpressionTarget(arrow.Expression, members, out target);
        }

        return TryBlockTarget(block, members, out target);
    }

    public static bool TryExpressionTarget(ExpressionSyntax expression, HashSet<string> members, out string? target)
    {
        var expr = Unwrap(expression);
        if (expr is AssignmentExpressionSyntax assignment)
        {
            return TryChainTarget(assignment.Left, members, out target)
                && IsAllowedArgument(assignment.Right, members);
        }

        if (expr is InvocationExpressionSyntax invocation)
        {
            return TryChainTarget(invocation.Expression, members, out target)
                && invocation.ArgumentList.Arguments.All(argument => IsAllowedArgument(argument.Expression, members));
        }

        return TryChainTarget(expr, members, out target);
    }

    private static bool TryBlockTarget(BlockSyntax? block, HashSet<string> members, out string? target)
    {
        target = null;
        if (block == null || block.Statements.Count == 0)
        {
            return false;
        }

        string? agreed = null;
        foreach (var statement in block.Statements)
        {
            if (!TryStatementTarget(statement, members, out var statementTarget) || !Agree(ref agreed, statementTarget))
            {
                return false;
            }
        }

        target = agreed;
        return agreed != null;
    }

    private static bool TryStatementTarget(StatementSyntax statement, HashSet<string> members, out string? target)
    {
        if (statement is ReturnStatementSyntax { Expression: { } returned })
        {
            return TryExpressionTarget(returned, members, out target);
        }

        if (statement is ExpressionStatementSyntax expression)
        {
            return TryExpressionTarget(expression.Expression, members, out target);
        }

        target = null;
        return false;
    }

    private static bool TryChainTarget(ExpressionSyntax called, HashSet<string> members, out string? target)
    {
        target = null;
        var expr = Unwrap(called);
        if (expr is not MemberAccessExpressionSyntax)
        {
            return false;
        }

        expr = Leftmost(expr, members, out var thisMember);
        if (thisMember != null)
        {
            if (thisMember.Length == 0)
            {
                return false;
            }

            target = thisMember;
            return true;
        }

        if (expr is IdentifierNameSyntax identifier && members.Contains(identifier.Identifier.ValueText))
        {
            target = identifier.Identifier.ValueText;
            return true;
        }

        return false;
    }

    private static ExpressionSyntax Leftmost(ExpressionSyntax expr, HashSet<string> members, out string? thisMember)
    {
        thisMember = null;
        while (expr is MemberAccessExpressionSyntax access)
        {
            var next = Unwrap(access.Expression);
            if (next is ThisExpressionSyntax)
            {
                var name = access.Name.Identifier.ValueText;
                thisMember = members.Contains(name) ? name : "";
                return expr;
            }

            expr = next;
        }

        return expr;
    }

    private static bool IsAllowedArgument(ExpressionSyntax expression, HashSet<string> members)
    {
        var expr = Unwrap(expression);
        if (expr is LiteralExpressionSyntax or DefaultExpressionSyntax)
        {
            return true;
        }

        if (expr is IdentifierNameSyntax identifier)
        {
            return !members.Contains(identifier.Identifier.ValueText);
        }

        if (expr is PrefixUnaryExpressionSyntax unary)
        {
            return IsAllowedArgument(unary.Operand, members);
        }

        if (expr is CastExpressionSyntax cast)
        {
            return IsAllowedArgument(cast.Expression, members);
        }

        return IsNameof(expr);
    }

    private static bool IsNameof(ExpressionSyntax expr)
    {
        return expr is InvocationExpressionSyntax
        {
            Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" },
        };
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        var current = expression;
        while (current is AwaitExpressionSyntax awaited)
        {
            current = awaited.Expression;
        }

        while (current is ParenthesizedExpressionSyntax parenthesized)
        {
            current = parenthesized.Expression;
        }

        if (current is AwaitExpressionSyntax or ParenthesizedExpressionSyntax)
        {
            return Unwrap(current);
        }

        return current;
    }

    private static bool Agree(ref string? agreed, string? next)
    {
        if (next == null || (agreed != null && agreed != next))
        {
            return false;
        }

        agreed ??= next;
        return true;
    }
}
