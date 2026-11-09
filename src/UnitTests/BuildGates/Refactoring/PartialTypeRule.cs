using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags <c>partial</c> types, the move that spreads one oversized class over several files. A Blazor code-behind
/// (<c>X.razor.cs</c> next to <c>X.razor</c>, declaring <c>X</c>) and a type a source generator needs to be partial
/// are not violations.
/// </summary>
public sealed class PartialTypeRule : IStructureRule
{
    private const string CodeBehindSuffix = ".razor.cs";

    private static readonly string[] SourceGeneratorAttributes = ["GeneratedRegex", "LoggerMessage", "JsonSerializable"];

    public string BaselineFileName => "partial-types.txt";

    public string Guidance =>
        "A partial type hides a Large Class instead of fixing it. Extract the concept into its own class instead.";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files
            .SelectMany(file => file.TypeDeclarations
                .OfType<TypeDeclarationSyntax>()
                .Where(type => type.Modifiers.Any(SyntaxKind.PartialKeyword))
                .Where(type => !IsBlazorCodeBehind(file, type, sources) && !IsRequiredBySourceGenerator(type))
                .Select(file.ViolationAt))
            .Distinct();

    private static bool IsBlazorCodeBehind(SourceFile file, TypeDeclarationSyntax type, SourceSet sources)
    {
        if (!file.RelativePath.EndsWith(CodeBehindSuffix, StringComparison.OrdinalIgnoreCase)
            || type.Parent is TypeDeclarationSyntax)
        {
            return false;
        }

        var componentPath = file.RelativePath[..^".cs".Length];
        var componentName = Path.GetFileName(file.RelativePath[..^CodeBehindSuffix.Length]);
        return sources.RazorComponents.Contains(componentPath)
               && string.Equals(componentName, type.Identifier.Text, StringComparison.Ordinal);
    }

    private static bool IsRequiredBySourceGenerator(TypeDeclarationSyntax type) =>
        HasGeneratorAttribute(type.AttributeLists)
        || type.Members.Any(member =>
            member.Modifiers.Any(SyntaxKind.PartialKeyword) && HasGeneratorAttribute(member.AttributeLists));

    private static bool HasGeneratorAttribute(SyntaxList<AttributeListSyntax> attributeLists) =>
        attributeLists
            .SelectMany(list => list.Attributes)
            .Select(attribute => AttributeName(attribute.Name))
            .Any(name => SourceGeneratorAttributes.Contains(name, StringComparer.Ordinal));

    private static string AttributeName(NameSyntax name)
    {
        var simple = name is QualifiedNameSyntax qualified ? qualified.Right.Identifier.Text : name.ToString();
        return simple.EndsWith("Attribute", StringComparison.Ordinal) ? simple[..^"Attribute".Length] : simple;
    }
}
