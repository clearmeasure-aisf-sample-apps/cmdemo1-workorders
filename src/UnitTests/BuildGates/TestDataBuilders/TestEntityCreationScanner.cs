using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.TestDataBuilders;

/// <summary>
/// Finds <c>new WorkOrder</c> and <c>new Employee(</c> creations in test projects outside ObjectMother,
/// <c>*Mother</c> and <c>*Builder</c> types. Each hit is a ratchet key; the count may only decrease.
/// </summary>
internal static class TestEntityCreationScanner
{
    private static readonly string[] SupportSuffixes = ["Mother", "Builder"];

    /// <summary>Sorted creation keys for the hand-written test sources of <paramref name="checkout"/>.</summary>
    public static IReadOnlyList<string> ScanRepository(RepositoryCheckout checkout)
    {
        var keys = new List<string>();
        foreach (var path in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(path);
            if (!IsTestProjectPath(relative))
            {
                continue;
            }

            keys.AddRange(ScanFile(relative, File.ReadAllText(path)));
        }

        keys.Sort(StringComparer.Ordinal);
        return keys;
    }

    /// <summary>Creation keys in one source file.</summary>
    public static IReadOnlyList<string> ScanFile(string relativePath, string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        var supportSpans = SupportTypeSpans(root);
        var keys = new List<string>();
        var workOrderIndex = 0;
        var employeeIndex = 0;

        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (supportSpans.Any(span => span.Contains(creation.Span)))
            {
                continue;
            }

            if (IsWorkOrderCreation(creation))
            {
                workOrderIndex++;
                keys.Add($"WorkOrder {relativePath}#{workOrderIndex}");
            }
            else if (IsEmployeeConstructor(creation))
            {
                employeeIndex++;
                keys.Add($"Employee {relativePath}#{employeeIndex}");
            }
        }

        return keys;
    }

    private static bool IsTestProjectPath(string relativePath) =>
        relativePath.StartsWith("src/UnitTests/", StringComparison.Ordinal)
        || relativePath.StartsWith("src/IntegrationTests/", StringComparison.Ordinal)
        || relativePath.StartsWith("src/AcceptanceTests/", StringComparison.Ordinal);

    private static bool IsWorkOrderCreation(ObjectCreationExpressionSyntax creation) =>
        TypeName(creation) is "WorkOrder";

    private static bool IsEmployeeConstructor(ObjectCreationExpressionSyntax creation) =>
        TypeName(creation) is "Employee" && creation.ArgumentList is { Arguments.Count: > 0 };

    private static string? TypeName(ObjectCreationExpressionSyntax creation) =>
        creation.Type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            _ => null
        };

    private static List<TextSpan> SupportTypeSpans(SyntaxNode root) =>
        root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(IsTestSupportType)
            .Select(type => type.Span)
            .ToList();

    private static bool IsTestSupportType(TypeDeclarationSyntax type) =>
        type.Identifier.Text.Equals("ObjectMother", StringComparison.Ordinal)
        || SupportSuffixes.Any(suffix => type.Identifier.Text.EndsWith(suffix, StringComparison.Ordinal));
}
