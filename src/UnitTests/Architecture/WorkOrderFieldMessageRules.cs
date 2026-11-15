using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Roslyn helpers for the #1805 gate: work-order field messages live only in Core Model/Validation.
/// </summary>
internal static class WorkOrderFieldMessageRules
{
    /// <summary>
    /// Exact production files that still hold a non-work-order <c>is required</c> message.
    /// Folder wildcards are refused so a work-order field message cannot hide under them.
    /// </summary>
    private static readonly string[] AllowedIsRequiredFiles =
    [
        // RemotableBus API URL config for the Worker host — not a work-order field rule
        "src/Worker/WorkOrderEndpoint.cs",
        // Database CLI required-option messages for server/name/credentials
        "src/Database/Console/DatabaseOptions.cs",
        // Database CLI connection-string builder credential pairing message
        "src/Database/Console/DatabaseConnectionStringBuilder.cs",
        // Acceptance/test-support endpoint parameter messages (TestTag, userName)
        "src/UI/Server/TestSupport/TestSupportEndpoints.cs",
        // Anonymous tools API: required query/body field messages for random values
        "src/UI/Api/Controllers/ToolsRandomController.cs",
        // Anonymous tools API: required JSON text for slugify
        "src/UI/Api/Controllers/ToolsSlugifyController.cs",
        // Anonymous tools API: required JSON text for hash
        "src/UI/Api/Controllers/ToolsHashController.cs",
        // Anonymous tools API: required date query for due-date check
        "src/UI/Api/Controllers/ToolsDueDateCheckController.cs",
        // Anonymous tools API: required JSON text for case conversion
        "src/UI/Api/Controllers/ToolsCaseController.cs",
        // Anonymous tools API: required JSON text for base64
        "src/UI/Api/Controllers/ToolsBase64Controller.cs",
        // Login form employee picker — not a work-order field rule
        "src/UI.Shared/Pages/Login.razor.cs",
        // CSV upload presence check before parse — not a field-length rule
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
        AppendStringPhraseViolations(relativePath, root, violations);
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

    private static void AppendStringPhraseViolations(
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

            AppendBannedPhrases(relativePath, literal.Token.ValueText, violations);
        }

        foreach (var interpolated in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
        {
            AppendBannedPhrases(relativePath, InterpolatedPlainText(interpolated), violations);
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

    private static void AppendBannedPhrases(string relativePath, string text, List<string> violations)
    {
        if (text.Contains("cannot exceed", StringComparison.Ordinal))
        {
            violations.Add($"{relativePath}:cannot exceed");
        }

        if (text.Contains("is required", StringComparison.Ordinal) && !IsAllowedIsRequiredFile(relativePath))
        {
            violations.Add($"{relativePath}:is required");
        }
    }

    private static bool IsAllowedIsRequiredFile(string relativePath) =>
        AllowedIsRequiredFiles.Contains(relativePath, StringComparer.Ordinal);

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

            return IsHardCodedErrorMessageExpression(argument.Expression);
        }

        return false;
    }

    private static bool IsHardCodedErrorMessageExpression(ExpressionSyntax expression) =>
        expression is InterpolatedStringExpressionSyntax
        || (expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression));
}
