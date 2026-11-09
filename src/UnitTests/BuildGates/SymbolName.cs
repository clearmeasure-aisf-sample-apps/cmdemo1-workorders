using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Stable member key for the ratchet. Parameter names are omitted so a rename
/// does not churn the baseline. Type-parameter arity is included so overloads
/// that differ only by generics stay distinct.
/// </summary>
internal static class SymbolName
{
    /// <summary>Owner, member, and parameter types, for example <c>WorkOrder.Assign(Employee)</c>.</summary>
    public static string Format(SyntaxNode member, BaseParameterListSyntax? parameters)
    {
        return Head(member) + ParameterText(parameters);
    }

    private static string Head(SyntaxNode member)
    {
        if (member is CompilationUnitSyntax)
        {
            return "<top-level>";
        }

        var name = LocalName(member);
        var owner = Owner(member);
        return owner.Length == 0 ? name : owner + "." + name;
    }

    private static string Owner(SyntaxNode member)
    {
        var names = new List<string>();
        foreach (var ancestor in member.Ancestors())
        {
            var name = OwnerName(ancestor);
            if (name != null)
            {
                names.Add(name);
            }
        }

        names.Reverse();
        return string.Join(".", names);
    }

    private static string? OwnerName(SyntaxNode ancestor)
    {
        var named = SimpleName(ancestor);
        if (named != null)
        {
            return named;
        }

        return ancestor switch
        {
            ConstructorDeclarationSyntax constructor => ConstructorName(constructor),
            DestructorDeclarationSyntax destructor => "~" + destructor.Identifier.ValueText,
            OperatorDeclarationSyntax op => "operator " + op.OperatorToken.Text,
            ConversionOperatorDeclarationSyntax conversion => ConversionName(conversion),
            _ => null
        };
    }

    private static string LocalName(SyntaxNode member)
    {
        var named = SimpleName(member);
        if (named != null)
        {
            return named;
        }

        if (member is ConstructorDeclarationSyntax constructor)
        {
            return ConstructorName(constructor);
        }

        if (member is DestructorDeclarationSyntax destructor)
        {
            return "~" + destructor.Identifier.ValueText;
        }

        if (member is OperatorDeclarationSyntax op)
        {
            return "operator " + op.OperatorToken.Text;
        }

        if (member is ConversionOperatorDeclarationSyntax conversion)
        {
            return ConversionName(conversion);
        }

        return member.Kind().ToString();
    }

    private static string? SimpleName(SyntaxNode node)
    {
        return MethodLikeName(node) ?? TypeLikeName(node);
    }

    private static string? MethodLikeName(SyntaxNode node)
    {
        if (node is MethodDeclarationSyntax method)
        {
            return ExplicitPrefix(method) + method.Identifier.ValueText + Arity(method.TypeParameterList);
        }

        if (node is LocalFunctionStatementSyntax local)
        {
            return local.Identifier.ValueText + Arity(local.TypeParameterList);
        }

        if (node is AccessorDeclarationSyntax accessor)
        {
            return accessor.Keyword.Text;
        }

        return null;
    }

    private static string? TypeLikeName(SyntaxNode node)
    {
        if (node is PropertyDeclarationSyntax property)
        {
            return property.Identifier.ValueText;
        }

        if (node is TypeDeclarationSyntax type)
        {
            return type.Identifier.ValueText + Arity(type.TypeParameterList);
        }

        if (node is DelegateDeclarationSyntax del)
        {
            return del.Identifier.ValueText + Arity(del.TypeParameterList);
        }

        if (node is IndexerDeclarationSyntax)
        {
            return "this";
        }

        return null;
    }

    private static string ConstructorName(ConstructorDeclarationSyntax constructor)
    {
        var typeName = TypeName(constructor);
        var isStatic = constructor.Modifiers.Any(token => token.Text == "static");
        return isStatic ? "static " + typeName : typeName;
    }

    private static string ConversionName(ConversionOperatorDeclarationSyntax conversion)
    {
        return "operator " + conversion.ImplicitOrExplicitKeyword.Text;
    }

    private static string ExplicitPrefix(MethodDeclarationSyntax method)
    {
        if (method.ExplicitInterfaceSpecifier is null)
        {
            return "";
        }

        return Collapse(method.ExplicitInterfaceSpecifier.Name.ToString()) + ".";
    }

    private static string TypeName(SyntaxNode node)
    {
        var type = node.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        return type is null ? ".ctor" : type.Identifier.ValueText + Arity(type.TypeParameterList);
    }

    private static string Arity(TypeParameterListSyntax? typeParameters)
    {
        if (typeParameters is null || typeParameters.Parameters.Count == 0)
        {
            return "";
        }

        return "<" + typeParameters.Parameters.Count + ">";
    }

    private static string ParameterText(BaseParameterListSyntax? parameters)
    {
        if (parameters is null)
        {
            return "()";
        }

        var parts = new string[parameters.Parameters.Count];
        for (var i = 0; i < parameters.Parameters.Count; i++)
        {
            parts[i] = FormatParameter(parameters.Parameters[i]);
        }

        return "(" + string.Join(",", parts) + ")";
    }

    private static string FormatParameter(ParameterSyntax parameter)
    {
        var modifiers = parameter.Modifiers.Count == 0 ? "" : parameter.Modifiers + " ";
        var type = parameter.Type is null ? "" : Collapse(parameter.Type.ToString());
        return (modifiers + type).Trim();
    }

    private static string Collapse(string text)
    {
        var parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join("", parts);
    }
}
