using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags static classes named <c>*Helper(s)</c>, <c>*Util(s)</c>, <c>*Utility</c> or <c>*Utilities</c>.
/// </summary>
public sealed partial class StaticHelperClassRule : IStructureRule
{
    public string BaselineFileName => "static-helper-classes.txt";

    public string Guidance =>
        "A static helper names no concept. Move each function onto the type it envies, or extract a named domain " +
        "service or value object.";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files.SelectMany(file => file.TypeDeclarations
            .OfType<ClassDeclarationSyntax>()
            .Where(type => type.Modifiers.Any(SyntaxKind.StaticKeyword)
                           && HelperName().IsMatch(type.Identifier.Text))
            .Select(file.ViolationAt));

    [GeneratedRegex("(Helper|Helpers|Util|Utils|Utility|Utilities)$")]
    private static partial Regex HelperName();
}
