using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

internal static partial class ArchitectureRules
{
    /// <summary>
    /// Namespaces that directly contain a type. Empty when the file declares no namespace.
    /// </summary>
    private static IReadOnlyList<string> DeclaredNamespaces(string source)
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
    private static IReadOnlyList<string> PublicTopLevelTypeNames(string source)
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

    /// <summary>
    /// Method and property names whose declared return type is <c>Expression&lt;Func&lt;WorkOrder, bool&gt;&gt;</c>.
    /// Work-order bool predicates belong in Core <c>WorkOrderSpecifications</c>.
    /// </summary>
    internal static IReadOnlyList<string> WorkOrderBoolExpressionMembers(string source) =>
        WorkOrderBoolExpressionMembers(Parse(source));

    /// <summary>
    /// Method and property names whose declared return type is <c>Expression&lt;Func&lt;WorkOrder, bool&gt;&gt;</c>.
    /// </summary>
    private static IReadOnlyList<string> WorkOrderBoolExpressionMembers(CompilationUnitSyntax root)
    {
        var names = new List<string>();
        foreach (var member in root.DescendantNodes())
        {
            if (member is MethodDeclarationSyntax method && IsWorkOrderBoolExpression(method.ReturnType))
            {
                names.Add(method.Identifier.ValueText);
            }
            else if (member is PropertyDeclarationSyntax property && IsWorkOrderBoolExpression(property.Type))
            {
                names.Add(property.Identifier.ValueText);
            }
        }

        return SortedDistinct(names);
    }

    private static bool IsWorkOrderBoolExpression(TypeSyntax type)
    {
        if (type is not GenericNameSyntax { Identifier.ValueText: "Expression", TypeArgumentList.Arguments.Count: 1 } expression)
        {
            return false;
        }

        if (expression.TypeArgumentList.Arguments[0] is not GenericNameSyntax
            {
                Identifier.ValueText: "Func", TypeArgumentList.Arguments.Count: 2
            } func)
        {
            return false;
        }

        var workOrder = SimpleTypeName(func.TypeArgumentList.Arguments[0]);
        return workOrder is "WorkOrder" && IsBoolType(func.TypeArgumentList.Arguments[1]);
    }

    private static bool IsBoolType(TypeSyntax type) =>
        type is PredefinedTypeSyntax predefined
            ? predefined.Keyword.IsKind(SyntaxKind.BoolKeyword)
            : SimpleTypeName(type) is "Boolean";

    /// <summary>
    /// True when the source references <c>DbContext</c> or <c>DataContext</c> as a type name
    /// (identifier, typeof, generic type argument, or using of EntityFrameworkCore).
    /// </summary>
    internal static bool ReferencesDbContext(string source) =>
        ReferencesDbContext(Parse(source));

    /// <summary>
    /// True when the parsed tree references a persistence context type or EF Core namespace.
    /// </summary>
    private static bool ReferencesDbContext(CompilationUnitSyntax root)
    {
        foreach (var usingDirective in root.Usings)
        {
            if (usingDirective.Name?.ToString().Contains("EntityFrameworkCore", StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        foreach (var identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var name = identifier.Identifier.ValueText;
            if (name is "DbContext" or "DataContext")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Constructor declarations that invoke <c>CreateScope()</c> (scope leak smell).
    /// </summary>
    internal static IReadOnlyList<string> ConstructorsCallingCreateScope(string source) =>
        ConstructorsCallingCreateScope(Parse(source));

    /// <summary>
    /// Constructor declarations that invoke <c>CreateScope()</c>.
    /// </summary>
    private static IReadOnlyList<string> ConstructorsCallingCreateScope(CompilationUnitSyntax root)
    {
        var names = new List<string>();
        foreach (var ctor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            if (ctor.Body is null && ctor.ExpressionBody is null)
            {
                continue;
            }

            if (CallsCreateScope(ctor))
            {
                names.Add(ctor.Identifier.ValueText);
            }
        }

        return SortedDistinct(names);
    }

    private static bool CallsCreateScope(ConstructorDeclarationSyntax ctor)
    {
        foreach (var invocation in ctor.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (IsCreateScopeCall(invocation))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCreateScopeCall(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            IdentifierNameSyntax { Identifier.ValueText: "CreateScope" } => true,
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CreateScope" } => true,
            _ => false
        };
    }
}
