using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

internal static partial class ArchitectureRules
{
    /// <summary>
    /// Namespaces that directly contain a type. Empty when the file declares no namespace.
    /// </summary>
    internal static IReadOnlyList<string> DeclaredNamespaces(string source)
    {
        return DeclaredNamespaces(Parse(source));
    }

    /// <summary>
    /// Namespaces that directly contain a type. Empty when the file declares no namespace.
    /// </summary>
    internal static IReadOnlyList<string> DeclaredNamespaces(CompilationUnitSyntax root)
    {
        var names = new List<string>();
        CollectNamespaces(root.Members, "", names);
        return SortedDistinct(names);
    }

    /// <summary>
    /// True when every declared namespace equals the folder namespace.
    /// A file with no namespace (global usings, top-level statements) matches.
    /// </summary>
    internal static bool NamespaceMatchesFolder(string source, string rootNamespace, IReadOnlyList<string> folderSegments)
    {
        var declared = DeclaredNamespaces(source);
        if (declared.Count == 0)
        {
            return true;
        }

        var expected = ExpectedNamespace(rootNamespace, folderSegments);
        foreach (var ns in declared)
        {
            if (ns != expected)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Distinct public top-level type names. Nested types and non-public types are ignored.
    /// </summary>
    internal static IReadOnlyList<string> PublicTopLevelTypeNames(string source)
    {
        return PublicTopLevelTypeNames(Parse(source));
    }

    /// <summary>
    /// Distinct public top-level type names. Nested types and non-public types are ignored.
    /// </summary>
    internal static IReadOnlyList<string> PublicTopLevelTypeNames(CompilationUnitSyntax root)
    {
        var names = new List<string>();
        CollectPublicTypes(root.Members, names);
        return SortedDistinct(names);
    }

    /// <summary>
    /// True when the file declares more than one public top-level type.
    /// </summary>
    internal static bool HasMultiplePublicTypes(string source)
    {
        return PublicTopLevelTypeNames(source).Count > 1;
    }

    /// <summary>
    /// Handler type names whose base list names a MediatR handler interface and whose name does not end in <c>Handler</c>.
    /// </summary>
    internal static IReadOnlyList<string> MisnamedHandlerTypeNames(string source)
    {
        return MisnamedHandlerTypeNames(Parse(source));
    }

    /// <summary>
    /// Handler type names whose base list names a MediatR handler interface and whose name does not end in <c>Handler</c>.
    /// </summary>
    internal static IReadOnlyList<string> MisnamedHandlerTypeNames(CompilationUnitSyntax root)
    {
        var names = new List<string>();
        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (type is InterfaceDeclarationSyntax)
            {
                continue;
            }

            if (IsMediatRHandler(type) && !type.Identifier.ValueText.EndsWith("Handler", StringComparison.Ordinal))
            {
                names.Add(type.Identifier.ValueText);
            }
        }

        return SortedDistinct(names);
    }

    internal static CompilationUnitSyntax Parse(string source)
    {
        return CSharpSyntaxTree.ParseText(StripBom(source)).GetCompilationUnitRoot();
    }

    private static void CollectNamespaces(SyntaxList<MemberDeclarationSyntax> members, string prefix, List<string> names)
    {
        foreach (var member in members)
        {
            if (member is not BaseNamespaceDeclarationSyntax ns)
            {
                continue;
            }

            var name = NamespaceName(ns.Name);
            var fullName = prefix.Length == 0 ? name : prefix + "." + name;
            if (ContainsDirectType(ns.Members))
            {
                names.Add(fullName);
            }

            CollectNamespaces(ns.Members, fullName, names);
        }
    }

    private static bool ContainsDirectType(SyntaxList<MemberDeclarationSyntax> members)
    {
        foreach (var member in members)
        {
            if (member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            {
                return true;
            }
        }

        return false;
    }

    private static void CollectPublicTypes(SyntaxList<MemberDeclarationSyntax> members, List<string> names)
    {
        foreach (var member in members)
        {
            if (member is BaseNamespaceDeclarationSyntax ns)
            {
                CollectPublicTypes(ns.Members, names);
                continue;
            }

            if (member is BaseTypeDeclarationSyntax type && IsPublic(type))
            {
                names.Add(type.Identifier.ValueText);
                continue;
            }

            if (member is DelegateDeclarationSyntax delegateType && IsPublic(delegateType))
            {
                names.Add(delegateType.Identifier.ValueText);
            }
        }
    }

    private static bool IsPublic(MemberDeclarationSyntax member)
    {
        foreach (var modifier in member.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PublicKeyword))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMediatRHandler(TypeDeclarationSyntax type)
    {
        if (type.BaseList == null)
        {
            return false;
        }

        foreach (var baseType in type.BaseList.Types)
        {
            var name = SimpleTypeName(baseType.Type);
            if (name is "IRequestHandler" or "INotificationHandler" or "IStreamRequestHandler")
            {
                return true;
            }
        }

        return false;
    }

    private static string SimpleTypeName(TypeSyntax type)
    {
        if (type is GenericNameSyntax generic)
        {
            return generic.Identifier.ValueText;
        }

        if (type is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        if (type is QualifiedNameSyntax qualified)
        {
            return SimpleTypeName(qualified.Right);
        }

        if (type is AliasQualifiedNameSyntax alias)
        {
            return SimpleTypeName(alias.Name);
        }

        return "";
    }

    private static string NamespaceName(NameSyntax name)
    {
        if (name is QualifiedNameSyntax qualified)
        {
            return NamespaceName(qualified.Left) + "." + NamespaceName(qualified.Right);
        }

        if (name is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        if (name is AliasQualifiedNameSyntax alias)
        {
            return alias.Name.Identifier.ValueText;
        }

        return name.ToString();
    }
}
