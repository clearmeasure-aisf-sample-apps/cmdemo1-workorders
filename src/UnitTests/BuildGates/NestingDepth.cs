using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Counts nested control-flow statements. The member itself is depth 0.
/// <c>else if</c> stays at the depth of the <c>if</c> it continues.
/// A local function is measured on its own and does not deepen the outer member.
/// </summary>
internal static class NestingDepth
{
    /// <summary>Deepest control-flow nesting inside <paramref name="callable"/>.</summary>
    public static int Measure(SyntaxNode callable)
    {
        return Walk(callable, 0, isRoot: true);
    }

    private static int Walk(SyntaxNode node, int depth, bool isRoot)
    {
        if (!isRoot && node is LocalFunctionStatementSyntax)
        {
            return depth;
        }

        var here = !isRoot && IsNesting(node) ? depth + 1 : depth;
        var max = here;
        foreach (var child in node.ChildNodes())
        {
            var childMax = Walk(child, here, isRoot: false);
            if (childMax > max)
            {
                max = childMax;
            }
        }

        return max;
    }

    private static bool IsNesting(SyntaxNode node)
    {
        if (node is IfStatementSyntax { Parent: ElseClauseSyntax })
        {
            return false;
        }

        return node.Kind() is
            SyntaxKind.IfStatement or
            SyntaxKind.ForStatement or
            SyntaxKind.ForEachStatement or
            SyntaxKind.ForEachVariableStatement or
            SyntaxKind.WhileStatement or
            SyntaxKind.DoStatement or
            SyntaxKind.SwitchStatement or
            SyntaxKind.TryStatement or
            SyntaxKind.LockStatement or
            SyntaxKind.UsingStatement or
            SyntaxKind.FixedStatement;
    }
}
