using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags static classes that hold extension methods (or C# 14 <c>extension(...)</c> blocks) whose receiver is a
/// type declared in this repository. Behavior of our own types belongs on those types.
/// </summary>
public sealed partial class OwnTypeExtensionRule : IStructureRule
{
    public string BaselineFileName => "own-type-extensions.txt";

    public string Guidance =>
        "An extension method on our own type moves its behavior away from it. Move the method onto the type " +
        "(Move Function), or extract a value object or domain service that owns it.";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files.SelectMany(file => file.Classes
            .Where(type => type.Modifiers.Any(SyntaxKind.StaticKeyword))
            .Where(type => ReceiverNames(type).Any(sources.DeclaredTypeNames.Contains))
            .Select(file.ViolationAt));

    private static IEnumerable<string> ReceiverNames(ClassDeclarationSyntax type)
    {
        foreach (var method in type.Members.OfType<MethodDeclarationSyntax>())
        {
            var receiver = method.ParameterList.Parameters.FirstOrDefault();
            var isExtension = receiver?.Modifiers.Any(SyntaxKind.ThisKeyword) == true;
            var isTypeParameter = method.TypeParameterList?.Parameters
                .Any(parameter => parameter.Identifier.Text == SimpleName(receiver?.Type)) == true;
            if (isExtension && !isTypeParameter && SimpleName(receiver!.Type) is { } name)
            {
                yield return name;
            }
        }

        foreach (Match block in ExtensionBlock().Matches(type.ToFullString()))
        {
            yield return block.Groups["receiver"].Value.Split('.')[^1];
        }
    }

    private static string? SimpleName(TypeSyntax? type) => type switch
    {
        NullableTypeSyntax nullable => SimpleName(nullable.ElementType),
        QualifiedNameSyntax qualified => SimpleName(qualified.Right),
        AliasQualifiedNameSyntax alias => SimpleName(alias.Name),
        GenericNameSyntax generic => generic.Identifier.Text,
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        _ => null
    };

    [GeneratedRegex(@"(?<![\w.])extension\s*(<[^>]*>)?\s*\(\s*(?<receiver>[\w.]+)\??\s+\w+\s*\)")]
    private static partial Regex ExtensionBlock();
}
