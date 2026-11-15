using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Deterministic gate for #1812: Core must not contain CSS class literals or *Css* members.
/// </summary>
internal static class CorePresentationRules
{
    private static readonly string[] CssLikeFragments = ["btn-", "badge", "--", "due-date-"];

    /// <summary>
    /// CSS-like string literals and *Css* / *CssClass* member names in a Core source file.
    /// Empty when <paramref name="relativePath"/> is not under <c>src/Core/</c>.
    /// </summary>
    internal static IReadOnlyList<string> Violations(string relativePath, string source)
    {
        if (!relativePath.StartsWith("src/Core/", StringComparison.Ordinal)
            || !relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var violations = new List<string>();
        foreach (var text in StringLiteralValues(root))
        {
            if (LooksLikeCss(text))
            {
                violations.Add($"{relativePath}:literal:{text}");
            }
        }

        foreach (var name in DeclaredNames(root))
        {
            if (name.Contains("Css", StringComparison.Ordinal))
            {
                violations.Add($"{relativePath}:member:{name}");
            }
        }

        return violations;
    }

    private static IEnumerable<string> StringLiteralValues(CompilationUnitSyntax root)
    {
        foreach (var node in root.DescendantNodes())
        {
            if (node is LiteralExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.StringLiteralExpression
                } literal)
            {
                var text = literal.Token.ValueText;
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
    }

    private static IEnumerable<string> DeclaredNames(CompilationUnitSyntax root)
    {
        foreach (var type in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            yield return type.Identifier.ValueText;
        }

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            yield return method.Identifier.ValueText;
        }

        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            yield return property.Identifier.ValueText;
        }

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            foreach (var variable in field.Declaration.Variables)
            {
                yield return variable.Identifier.ValueText;
            }
        }
    }

    private static bool LooksLikeCss(string text)
    {
        foreach (var fragment in CssLikeFragments)
        {
            if (text.Contains(fragment, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
