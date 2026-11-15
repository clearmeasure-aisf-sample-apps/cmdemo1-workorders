using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.TestDataBuilders;

/// <summary>
/// Finds <c>new WorkOrder</c> and <c>new Employee(</c> creations in test projects outside ObjectMother,
/// <c>*Mother</c> and <c>*Builder</c> types, including target-typed <c>new()</c>. Each hit is a ratchet
/// key; the count may only decrease.
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

        foreach (var node in root.DescendantNodes())
        {
            if (supportSpans.Any(span => span.Contains(node.Span)))
            {
                continue;
            }

            if (IsWorkOrderCreation(node))
            {
                workOrderIndex++;
                keys.Add($"WorkOrder {relativePath}#{workOrderIndex}");
            }
            else if (IsEmployeeConstructor(node))
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

    private static bool IsWorkOrderCreation(SyntaxNode node) =>
        CreatedTypeName(node) is "WorkOrder";

    private static bool IsEmployeeConstructor(SyntaxNode node) =>
        CreatedTypeName(node) is "Employee" && HasConstructorArguments(node);

    private static bool HasConstructorArguments(SyntaxNode node) =>
        node switch
        {
            ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,
            ImplicitObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: > 0 } => true,
            _ => false
        };

    private static string? CreatedTypeName(SyntaxNode node) =>
        node switch
        {
            ObjectCreationExpressionSyntax creation => TypeName(creation.Type),
            ImplicitObjectCreationExpressionSyntax creation => InferredTypeName(creation),
            _ => null
        };

    private static string? InferredTypeName(ImplicitObjectCreationExpressionSyntax creation) =>
        creation.Parent switch
        {
            EqualsValueClauseSyntax equals => TypeFromEquals(equals),
            ArrowExpressionClauseSyntax arrow => TypeFromArrow(arrow),
            ReturnStatementSyntax => TypeFromEnclosingCallable(creation),
            _ => null
        };

    private static string? TypeFromEquals(EqualsValueClauseSyntax equals) =>
        equals.Parent switch
        {
            VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration }
                => TypeName(declaration.Type),
            PropertyDeclarationSyntax property => TypeName(property.Type),
            _ => null
        };

    private static string? TypeFromArrow(ArrowExpressionClauseSyntax arrow) =>
        arrow.Parent switch
        {
            MethodDeclarationSyntax method => TypeName(method.ReturnType),
            PropertyDeclarationSyntax property => TypeName(property.Type),
            LocalFunctionStatementSyntax local => TypeName(local.ReturnType),
            _ => null
        };

    private static string? TypeFromEnclosingCallable(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case MethodDeclarationSyntax method:
                    return TypeName(method.ReturnType);
                case LocalFunctionStatementSyntax local:
                    return TypeName(local.ReturnType);
                case PropertyDeclarationSyntax property:
                    return TypeName(property.Type);
            }
        }

        return null;
    }

    private static string? TypeName(TypeSyntax? type) =>
        type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            NullableTypeSyntax { ElementType: var element } => TypeName(element),
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
