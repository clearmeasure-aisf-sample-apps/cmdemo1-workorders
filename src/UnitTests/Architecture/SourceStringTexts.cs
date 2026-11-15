using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Plain string values from a C# compilation unit: ordinary string literals and the text parts of
/// interpolated strings (holes omitted), shared by architecture gates that scan source text.
/// </summary>
internal static class SourceStringTexts
{
    /// <summary>
    /// Non-empty string literal values and concatenated interpolated-string text parts.
    /// </summary>
    internal static IEnumerable<string> From(CompilationUnitSyntax root)
    {
        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                continue;
            }

            var text = literal.Token.ValueText;
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }

        foreach (var interpolated in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
        {
            var text = InterpolatedPlainText(interpolated);
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }
    }

    private static string InterpolatedPlainText(InterpolatedStringExpressionSyntax interpolated)
    {
        var text = new StringBuilder();
        foreach (var content in interpolated.Contents)
        {
            if (content is InterpolatedStringTextSyntax plain)
            {
                text.Append(plain.TextToken.ValueText);
            }
        }

        return text.ToString();
    }
}
