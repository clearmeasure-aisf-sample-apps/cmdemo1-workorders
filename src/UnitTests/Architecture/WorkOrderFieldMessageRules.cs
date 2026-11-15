using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Roslyn helpers for the #1805 gate: work-order field messages live only in Core Model/Validation.
/// </summary>
internal static class WorkOrderFieldMessageRules
{
    private static readonly string[] AllowedIsRequiredPrefixes =
    [
        "src/Database/",
        "src/UI/Api/Controllers/Tools",
        "src/UI/Server/TestSupport/",
        "src/Worker/",
        "src/UI.Shared/Pages/Login.razor.cs",
        "src/UI/Api/WorkOrderBulkImportProcessor.cs",
    ];

    /// <summary>
    /// Production sources outside Core/Model and Core/Validation that embed banned field-message literals
    /// or a hard-coded <c>ErrorMessage</c> on <c>StringLength</c>/<c>MaxLength</c>.
    /// </summary>
    internal static IReadOnlyList<string> Violations(string relativePath, string source)
    {
        if (!IsScannedProduction(relativePath))
        {
            return [];
        }

        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var violations = new List<string>();
        AppendLiteralViolations(relativePath, root, violations);
        AppendHardCodedLengthAttributeViolations(relativePath, root, violations);
        return violations;
    }

    private static bool IsScannedProduction(string relativePath)
    {
        if (relativePath.StartsWith("src/UnitTests/", StringComparison.Ordinal)
            || relativePath.StartsWith("src/IntegrationTests/", StringComparison.Ordinal)
            || relativePath.StartsWith("src/AcceptanceTests/", StringComparison.Ordinal))
        {
            return false;
        }

        return !relativePath.StartsWith("src/Core/Model/", StringComparison.Ordinal)
               && !relativePath.StartsWith("src/Core/Validation/", StringComparison.Ordinal);
    }

    private static void AppendLiteralViolations(
        string relativePath,
        CompilationUnitSyntax root,
        List<string> violations)
    {
        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                continue;
            }

            var text = literal.Token.ValueText;
            if (text.Contains("cannot exceed", StringComparison.Ordinal))
            {
                violations.Add($"{relativePath}:cannot exceed");
            }

            if (text.Contains("is required", StringComparison.Ordinal) && !IsAllowedIsRequiredPath(relativePath))
            {
                violations.Add($"{relativePath}:is required");
            }
        }
    }

    private static bool IsAllowedIsRequiredPath(string relativePath) =>
        AllowedIsRequiredPrefixes.Any(prefix =>
            relativePath.StartsWith(prefix, StringComparison.Ordinal)
            || string.Equals(relativePath, prefix, StringComparison.Ordinal));

    private static void AppendHardCodedLengthAttributeViolations(
        string relativePath,
        CompilationUnitSyntax root,
        List<string> violations)
    {
        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            if (!IsLengthAttribute(attribute) || !HasHardCodedErrorMessage(attribute))
            {
                continue;
            }

            violations.Add($"{relativePath}:hard-coded ErrorMessage on length attribute");
        }
    }

    private static bool IsLengthAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name is "StringLength" or "MaxLength"
               || name.EndsWith(".StringLength", StringComparison.Ordinal)
               || name.EndsWith(".MaxLength", StringComparison.Ordinal);
    }

    private static bool HasHardCodedErrorMessage(AttributeSyntax attribute)
    {
        if (attribute.ArgumentList == null)
        {
            return false;
        }

        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            if (argument.NameEquals?.Name.Identifier.ValueText != "ErrorMessage")
            {
                continue;
            }

            return argument.Expression is LiteralExpressionSyntax literal
                   && literal.IsKind(SyntaxKind.StringLiteralExpression);
        }

        return false;
    }
}
