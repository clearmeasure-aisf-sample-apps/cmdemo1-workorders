using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Deterministic UI.Shared conventions: non-nullable <c>[Inject]</c>, auth-provider injection
/// allowlist, and no bare <c>throw new Exception(</c>.
/// </summary>
internal static class UiSharedConventionRules
{
    private static readonly HashSet<string> AuthProviderTypeNames = new(StringComparer.Ordinal)
    {
        "AuthenticationStateProvider",
        "CustomAuthenticationStateProvider",
    };

    /// <summary>Names of <c>[Inject]</c> properties whose type is nullable.</summary>
    internal static IReadOnlyList<string> NullableInjectPropertyNames(string source)
    {
        var names = new List<string>();
        var root = ArchitectureRules.Parse(source);
        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (HasInjectAttribute(property) && property.Type is NullableTypeSyntax)
            {
                names.Add(property.Identifier.ValueText);
            }
        }

        return Sorted(names);
    }

    /// <summary>Auth-provider type names from <c>[Inject]</c> properties and constructors.</summary>
    internal static IReadOnlyList<string> AuthenticationProviderInjections(string source)
    {
        var root = ArchitectureRules.Parse(source);
        var names = new List<string>();
        CollectInjectedAuthProviders(root, names);
        CollectConstructedAuthProviders(root, names);
        return Sorted(names);
    }

    /// <summary>Auth-provider type names from <c>@inject</c> directives in a Razor document.</summary>
    internal static IReadOnlyList<string> AuthenticationProviderRazorInjects(string razorSource)
    {
        var names = new List<string>();
        foreach (var line in razorSource.Split('\n'))
        {
            CollectRazorInject(line.Trim(), names);
        }

        return Sorted(names);
    }

    /// <summary>True when the source throws the bare <c>Exception</c> type.</summary>
    internal static bool ThrowsBareException(string source)
    {
        var root = ArchitectureRules.Parse(source);
        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (IsBareExceptionType(creation.Type))
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectInjectedAuthProviders(CompilationUnitSyntax root, List<string> names)
    {
        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (!HasInjectAttribute(property))
            {
                continue;
            }

            AddIfAuthProvider(UnwrapNullable(property.Type), names);
        }
    }

    private static void CollectConstructedAuthProviders(CompilationUnitSyntax root, List<string> names)
    {
        foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>())
        {
            if (parameter.Type == null || !IsConstructorOrTypeParameter(parameter))
            {
                continue;
            }

            AddIfAuthProvider(UnwrapNullable(parameter.Type), names);
        }
    }

    private static bool IsConstructorOrTypeParameter(ParameterSyntax parameter) =>
        parameter.Parent is ParameterListSyntax
        {
            Parent: ConstructorDeclarationSyntax or ClassDeclarationSyntax
        };

    private static void AddIfAuthProvider(TypeSyntax type, List<string> names)
    {
        var typeName = SimpleTypeName(type);
        if (AuthProviderTypeNames.Contains(typeName))
        {
            names.Add(typeName);
        }
    }

    private static void CollectRazorInject(string trimmed, List<string> names)
    {
        if (!trimmed.StartsWith("@inject ", StringComparison.Ordinal))
        {
            return;
        }

        var parts = trimmed["@inject ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var typeName = parts[0].Split('.').Last();
        if (AuthProviderTypeNames.Contains(typeName))
        {
            names.Add(typeName);
        }
    }

    private static bool IsBareExceptionType(TypeSyntax type)
    {
        if (type is IdentifierNameSyntax { Identifier.ValueText: "Exception" })
        {
            return true;
        }

        return type is QualifiedNameSyntax qualified &&
               SimpleTypeName(qualified.Right) == "Exception" &&
               qualified.Left.ToString() is "System" or "global::System";
    }

    private static bool HasInjectAttribute(PropertyDeclarationSyntax property)
    {
        foreach (var list in property.AttributeLists)
        {
            foreach (var attribute in list.Attributes)
            {
                if (AttributeName(attribute) is "Inject" or "InjectAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string AttributeName(AttributeSyntax attribute) =>
        attribute.Name switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            QualifiedNameSyntax q => SimpleTypeName(q.Right),
            AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
            _ => attribute.Name.ToString()
        };

    private static TypeSyntax UnwrapNullable(TypeSyntax type) =>
        type is NullableTypeSyntax nullable ? nullable.ElementType : type;

    private static string SimpleTypeName(TypeSyntax type) =>
        type switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            GenericNameSyntax generic => generic.Identifier.ValueText,
            QualifiedNameSyntax qualified => SimpleTypeName(qualified.Right),
            AliasQualifiedNameSyntax alias => SimpleTypeName(alias.Name),
            _ => ""
        };

    private static IReadOnlyList<string> Sorted(IEnumerable<string> items) =>
        new List<string>(new SortedSet<string>(items, StringComparer.Ordinal));
}
