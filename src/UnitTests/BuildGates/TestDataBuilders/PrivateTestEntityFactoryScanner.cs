using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.TestDataBuilders;

/// <summary>
/// Finds private (or default-private) methods on test fixtures that return <c>WorkOrder</c>,
/// <c>Employee</c>, or arrays of those types. Async <c>Task&lt;…&gt;</c> query steps are left alone.
/// Test data must come from ObjectMother or builders, not fixture-local factories.
/// </summary>
internal static class PrivateTestEntityFactoryScanner
{
    private static readonly string[] SupportSuffixes = ["Mother", "Builder"];
    private static readonly string[] EntityNames = ["WorkOrder", "Employee"];
    private static readonly SyntaxKind[] AccessModifiers =
    [
        SyntaxKind.PublicKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword
    ];

    /// <summary>Sorted factory keys for the hand-written test sources of <paramref name="checkout"/>.</summary>
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

    /// <summary>Private fixture factory keys in one source file.</summary>
    public static IReadOnlyList<string> ScanFile(string relativePath, string text)
    {
        var file = SourceFile.Parse(relativePath, text);
        var keys = new List<string>();
        foreach (var type in file.Classes.Where(IsFixtureNotSupport))
        {
            foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
            {
                if (!IsPrivate(method) || !ReturnsEntityOrEntityArray(method))
                {
                    continue;
                }

                keys.Add($"{relativePath} :: {type.Identifier.Text}.{method.Identifier.Text}");
            }
        }

        return keys;
    }

    private static bool IsTestProjectPath(string relativePath) =>
        relativePath.StartsWith("src/UnitTests/", StringComparison.Ordinal)
        || relativePath.StartsWith("src/IntegrationTests/", StringComparison.Ordinal)
        || relativePath.StartsWith("src/AcceptanceTests/", StringComparison.Ordinal);

    private static bool IsFixtureNotSupport(ClassDeclarationSyntax type) =>
        NUnitSyntax.IsFixture(type) && !IsTestSupportType(type);

    private static bool IsTestSupportType(TypeDeclarationSyntax type) =>
        type.Identifier.Text.Equals("ObjectMother", StringComparison.Ordinal)
        || SupportSuffixes.Any(suffix => type.Identifier.Text.EndsWith(suffix, StringComparison.Ordinal));

    /// <summary>
    /// True when the method is private: the <c>private</c> keyword, or no access modifier
    /// (class members default to private).
    /// </summary>
    private static bool IsPrivate(MethodDeclarationSyntax method) =>
        method.Modifiers.Any(SyntaxKind.PrivateKeyword)
        || !method.Modifiers.Any(modifier => AccessModifiers.Contains(modifier.Kind()));

    private static bool ReturnsEntityOrEntityArray(MethodDeclarationSyntax method)
    {
        var type = method.ReturnType;
        if (type is ArrayTypeSyntax array)
        {
            type = array.ElementType;
        }

        var name = type switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            NullableTypeSyntax { ElementType: IdentifierNameSyntax identifier } => identifier.Identifier.Text,
            _ => null
        };
        return name is not null && EntityNames.Contains(name, StringComparer.Ordinal);
    }
}
