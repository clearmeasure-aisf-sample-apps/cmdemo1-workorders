using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// One C# file of the repository, parsed into a syntax tree. <see cref="RelativePath"/> uses forward slashes and
/// starts with <c>src/</c>.
/// </summary>
public sealed record SourceFile(string RelativePath, SyntaxNode Root)
{
    private static readonly string[] TestProjects = ["UnitTests", "IntegrationTests", "AcceptanceTests"];

    public static SourceFile Parse(string relativePath, string text) =>
        new(relativePath.Replace('\\', '/'), CSharpSyntaxTree.ParseText(text).GetRoot());

    /// <summary>The project folder under <c>src/</c>, for example <c>UnitTests</c>.</summary>
    private string Project => RelativePath.Split('/') is ["src", var project, ..] ? project : "";

    public bool IsTest => TestProjects.Contains(Project, StringComparer.Ordinal);

    public IEnumerable<BaseTypeDeclarationSyntax> TypeDeclarations =>
        Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>();

    public IEnumerable<ClassDeclarationSyntax> Classes => TypeDeclarations.OfType<ClassDeclarationSyntax>();

    public StructureViolation ViolationAt(BaseTypeDeclarationSyntax type) =>
        new(RelativePath, type.Identifier.Text);

    public StructureViolation ViolationAt(MethodDeclarationSyntax method) =>
        new(RelativePath, $"{(method.Parent as BaseTypeDeclarationSyntax)?.Identifier.Text}.{method.Identifier.Text}");
}
