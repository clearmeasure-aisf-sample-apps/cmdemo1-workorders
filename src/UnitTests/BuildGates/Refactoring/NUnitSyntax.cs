using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Recognizes NUnit tests, setup methods and assertions (NUnit, Shouldly, Playwright <c>Expect</c>) in a syntax tree.
/// </summary>
public static class NUnitSyntax
{
    private static readonly string[] TestAttributes = ["Test", "TestCase", "TestCaseSource", "Theory"];
    private static readonly string[] SetUpAttributes = ["SetUp", "OneTimeSetUp"];
    private static readonly string[] AssertionReceivers = ["Assert", "ClassicAssert", "Should"];

    private static bool IsTest(MethodDeclarationSyntax method) => HasAttribute(method.AttributeLists, TestAttributes);

    private static bool IsSetUp(MethodDeclarationSyntax method) => HasAttribute(method.AttributeLists, SetUpAttributes);

    public static bool IsFixture(ClassDeclarationSyntax type) =>
        HasAttribute(type.AttributeLists, ["TestFixture"])
        || type.Members.OfType<MethodDeclarationSyntax>().Any(IsTest);

    public static IEnumerable<MethodDeclarationSyntax> Tests(SourceFile file) =>
        file.Root.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsTest);

    public static IEnumerable<MethodDeclarationSyntax> SetUps(ClassDeclarationSyntax type) =>
        type.Members.OfType<MethodDeclarationSyntax>().Where(IsSetUp);

    public static bool ContainsAssertion(SyntaxNode node) =>
        node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().Any(IsAssertion);

    /// <summary>
    /// An assertion call. Skipping a test (<c>Assert.Ignore</c>, <c>Assert.Inconclusive</c>, <c>Assume</c>) is a
    /// precondition, not an assertion.
    /// </summary>
    private static bool IsAssertion(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Assume" } } => false,
        MemberAccessExpressionSyntax { Name.Identifier.Text: "Ignore" or "Inconclusive" } => false,
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver }
            when AssertionReceivers.Contains(receiver.Identifier.Text, StringComparer.Ordinal) => true,
        MemberAccessExpressionSyntax member => member.Name.Identifier.Text.StartsWith("Should", StringComparison.Ordinal),
        IdentifierNameSyntax { Identifier.Text: "Expect" } => true,
        _ => false
    };

    private static bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists, string[] names) =>
        attributeLists
            .SelectMany(list => list.Attributes)
            .Select(attribute => attribute.Name is QualifiedNameSyntax qualified
                ? qualified.Right.Identifier.Text
                : attribute.Name.ToString())
            .Select(name => name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name)
            .Any(name => names.Contains(name, StringComparer.Ordinal));
}
