using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

internal readonly record struct PassThroughFacts(bool SawForwarder, bool Disqualified, string? Target)
{
    public static PassThroughFacts None { get; } = new(false, false, null);
}

/// <summary>
/// A class is a pass-through when every instance method and property body only forwards to the same field,
/// property, or primary-constructor parameter.
/// </summary>
internal static class PassThroughSyntax
{
    public static PassThroughFacts Inspect(TypeDeclarationSyntax type)
    {
        var members = MemberNames(type);
        string? target = null;
        var sawForwarder = false;
        var disqualified = false;

        foreach (var member in type.Members)
        {
            var effect = Classify(member, members, out var memberTarget);
            if (effect == MemberEffect.Ignore)
            {
                continue;
            }

            if (effect == MemberEffect.Disqualify)
            {
                disqualified = true;
                continue;
            }

            sawForwarder = true;
            target = Agree(target, memberTarget, ref disqualified);
        }

        if (disqualified || !sawForwarder)
        {
            return new PassThroughFacts(false, disqualified, null);
        }

        return new PassThroughFacts(true, false, target);
    }

    private static MemberEffect Classify(
        MemberDeclarationSyntax member,
        HashSet<string> members,
        out string? target)
    {
        target = null;
        if (member is BaseMethodDeclarationSyntax method)
        {
            return ClassifyMethod(method, members, out target);
        }

        if (member is BasePropertyDeclarationSyntax property)
        {
            return ClassifyProperty(property, members, out target);
        }

        if (member is EventFieldDeclarationSyntax or EventDeclarationSyntax)
        {
            return MemberEffect.Disqualify;
        }

        return MemberEffect.Ignore;
    }

    private static MemberEffect ClassifyMethod(
        BaseMethodDeclarationSyntax method,
        HashSet<string> members,
        out string? target)
    {
        target = null;
        if (method is ConstructorDeclarationSyntax or DestructorDeclarationSyntax)
        {
            return MemberEffect.Ignore;
        }

        if (method.Body == null && method.ExpressionBody == null)
        {
            return HasModifier(method.Modifiers, SyntaxKind.PartialKeyword)
                ? MemberEffect.Ignore
                : MemberEffect.Disqualify;
        }

        return PassThroughExpressions.TryMemberTarget(method.ExpressionBody, method.Body, members, out target)
            ? MemberEffect.Forward
            : MemberEffect.Disqualify;
    }

    private static MemberEffect ClassifyProperty(
        BasePropertyDeclarationSyntax property,
        HashSet<string> members,
        out string? target)
    {
        target = null;
        var expressionBody = ExpressionBodyOf(property);
        if (expressionBody != null)
        {
            return PassThroughExpressions.TryExpressionTarget(expressionBody.Expression, members, out target)
                ? MemberEffect.Forward
                : MemberEffect.Disqualify;
        }

        if (property.AccessorList == null)
        {
            return MemberEffect.Disqualify;
        }

        return ClassifyAccessors(property.AccessorList, members, out target);
    }

    private static MemberEffect ClassifyAccessors(
        AccessorListSyntax accessors,
        HashSet<string> members,
        out string? target)
    {
        target = null;
        string? agreed = null;
        foreach (var accessor in accessors.Accessors)
        {
            if (accessor.Body == null && accessor.ExpressionBody == null)
            {
                return MemberEffect.Disqualify;
            }

            if (!PassThroughExpressions.TryMemberTarget(accessor.ExpressionBody, accessor.Body, members, out var accessorTarget))
            {
                return MemberEffect.Disqualify;
            }

            if (agreed != null && agreed != accessorTarget)
            {
                return MemberEffect.Disqualify;
            }

            agreed = accessorTarget;
        }

        target = agreed;
        return agreed == null ? MemberEffect.Disqualify : MemberEffect.Forward;
    }

    private static ArrowExpressionClauseSyntax? ExpressionBodyOf(BasePropertyDeclarationSyntax property)
    {
        if (property is PropertyDeclarationSyntax declared)
        {
            return declared.ExpressionBody;
        }

        if (property is IndexerDeclarationSyntax indexer)
        {
            return indexer.ExpressionBody;
        }

        return null;
    }

    private static HashSet<string> MemberNames(TypeDeclarationSyntax type)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in type.Members)
        {
            AddMemberName(member, names);
        }

        if (type.ParameterList == null)
        {
            return names;
        }

        foreach (var parameter in type.ParameterList.Parameters)
        {
            names.Add(parameter.Identifier.ValueText);
        }

        return names;
    }

    private static void AddMemberName(MemberDeclarationSyntax member, HashSet<string> names)
    {
        if (member is FieldDeclarationSyntax field)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                names.Add(variable.Identifier.ValueText);
            }
        }

        if (member is PropertyDeclarationSyntax property)
        {
            names.Add(property.Identifier.ValueText);
        }
    }

    private static string? Agree(string? current, string? next, ref bool disqualified)
    {
        if (next == null)
        {
            disqualified = true;
            return current;
        }

        if (current != null && !string.Equals(current, next, StringComparison.Ordinal))
        {
            disqualified = true;
        }

        return current ?? next;
    }

    private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
    {
        return modifiers.Any(token => token.IsKind(kind));
    }

    private enum MemberEffect
    {
        Ignore,
        Disqualify,
        Forward
    }
}
