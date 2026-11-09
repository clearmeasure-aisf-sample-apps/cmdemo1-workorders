using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Formats a base-type or alias target as a dotted name plus generic arity.
/// </summary>
internal static class TypeNameSyntax
{
    public static bool TryFormat(TypeSyntax type, out string name, out int arity)
    {
        if (TrySimple(type, out name, out arity) || TryComposite(type, out name, out arity))
        {
            return true;
        }

        name = "";
        arity = 0;
        return false;
    }

    private static bool TrySimple(TypeSyntax type, out string name, out int arity)
    {
        if (type is IdentifierNameSyntax identifier)
        {
            name = identifier.Identifier.ValueText;
            arity = 0;
            return true;
        }

        if (type is GenericNameSyntax generic)
        {
            name = generic.Identifier.ValueText;
            arity = generic.TypeArgumentList.Arguments.Count;
            return true;
        }

        name = "";
        arity = 0;
        return false;
    }

    private static bool TryComposite(TypeSyntax type, out string name, out int arity)
    {
        if (type is QualifiedNameSyntax qualified
            && TryFormat(qualified.Left, out var left, out _)
            && TryFormat(qualified.Right, out var right, out var rightArity))
        {
            name = left + "." + right;
            arity = rightArity;
            return true;
        }

        if (type is AliasQualifiedNameSyntax aliasQualified)
        {
            return TryFormat(aliasQualified.Name, out name, out arity);
        }

        if (type is NullableTypeSyntax nullable)
        {
            return TryFormat(nullable.ElementType, out name, out arity);
        }

        name = "";
        arity = 0;
        return false;
    }
}
