using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags a test file whose namespace is not <c>ClearMeasure.Bootcamp.&lt;Project&gt;.&lt;Folder&gt;...</c>, so a
/// category of fixtures lives in a folder and namespace of the same name.
/// </summary>
public sealed class TestNamespaceRule : IStructureRule
{
    public string BaselineFileName => "test-namespaces.txt";

    public string Guidance =>
        "A test namespace matches its folder. Group similar fixtures in a category folder and give them the " +
        "matching namespace (for example ClearMeasure.Bootcamp.UnitTests.WorkOrders.Assignment).";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files
            .Where(file => file.IsTest && file.TypeDeclarations.Any())
            .SelectMany(file => DeclaredNamespaces(file)
                .DefaultIfEmpty("(global)")
                .Where(declared => declared != ExpectedNamespace(file))
                .Select(declared => new StructureViolation(file.RelativePath, declared)));

    private static string ExpectedNamespace(SourceFile file)
    {
        var folders = file.RelativePath.Split('/')[1..^1];
        return string.Join('.', new[] { "ClearMeasure", "Bootcamp" }.Concat(folders));
    }

    private static IEnumerable<string> DeclaredNamespaces(SourceFile file) =>
        file.Root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(declaration => declaration.Name.ToString());
}
