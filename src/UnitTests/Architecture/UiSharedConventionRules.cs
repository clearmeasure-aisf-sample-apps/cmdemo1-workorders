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

    /// <summary>
    /// Line numbers of empty <c>catch</c> blocks (no statements; comments do not count).
    /// </summary>
    internal static IReadOnlyList<int> EmptyCatchLineNumbers(string source) =>
        EmptyCatchLineNumbers(ArchitectureRules.Parse(CSharpForCatchScan(source)));

    /// <summary>
    /// Line numbers of empty <c>catch</c> blocks in already-parsed C# (or a Razor <c>@code</c> wrapper).
    /// </summary>
    internal static IReadOnlyList<int> EmptyCatchLineNumbers(CompilationUnitSyntax root)
    {
        var lines = new List<int>();
        foreach (var clause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (clause.Block.Statements.Count == 0)
            {
                lines.Add(clause.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
            }
        }

        return lines;
    }

    /// <summary>True when the source mentions <c>IJSObjectReference</c> as a type name.</summary>
    internal static bool UsesIjsObjectReference(string source)
    {
        var root = ArchitectureRules.Parse(CSharpForCatchScan(source));
        foreach (var node in root.DescendantNodes())
        {
            if (node is IdentifierNameSyntax { Identifier.ValueText: "IJSObjectReference" })
            {
                return true;
            }

            if (node is QualifiedNameSyntax qualified && SimpleTypeName(qualified.Right) == "IJSObjectReference")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Line numbers of silent catch-alls: bare <c>catch</c> or <c>catch (Exception)</c> that neither
    /// logs nor rethrows (<c>throw;</c>).
    /// </summary>
    internal static IReadOnlyList<int> CatchExceptionWithoutLogLineNumbers(string source) =>
        CatchExceptionWithoutLogLineNumbers(ArchitectureRules.Parse(CSharpForCatchScan(source)));

    /// <summary>
    /// Line numbers of silent catch-alls in already-parsed C#.
    /// </summary>
    internal static IReadOnlyList<int> CatchExceptionWithoutLogLineNumbers(CompilationUnitSyntax root)
    {
        var lines = new List<int>();
        foreach (var clause in root.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (!IsSilentCatchAll(clause) || CatchBlockLogsOrRethrows(clause.Block))
            {
                continue;
            }

            lines.Add(clause.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
        }

        return lines;
    }

    /// <summary>
    /// C# text to scan for catch rules: plain <c>.cs</c> as-is, or each Razor <c>@code</c> block
    /// wrapped so Roslyn can parse it.
    /// </summary>
    internal static string CSharpForCatchScan(string source)
    {
        if (!source.Contains("@code", StringComparison.Ordinal))
        {
            return source;
        }

        var blocks = new List<string>();
        var index = 0;
        while (true)
        {
            var at = source.IndexOf("@code", index, StringComparison.Ordinal);
            if (at < 0)
            {
                break;
            }

            var open = source.IndexOf('{', at);
            if (open < 0)
            {
                break;
            }

            var close = MatchingBrace(source, open);
            if (close < 0)
            {
                break;
            }

            blocks.Add(source[(open + 1)..close]);
            index = close + 1;
        }

        if (blocks.Count == 0)
        {
            return source;
        }

        return "namespace RazorCodeScan { class Code { " + string.Join("\n", blocks) + " } }";
    }

    private static int MatchingBrace(string text, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static bool IsSilentCatchAll(CatchClauseSyntax clause)
    {
        if (clause.Declaration?.Type is null)
        {
            return true;
        }

        return SimpleTypeName(UnwrapNullable(clause.Declaration.Type)) == "Exception";
    }

    private static bool CatchBlockLogsOrRethrows(BlockSyntax block)
    {
        if (CatchBlockLogs(block))
        {
            return true;
        }

        foreach (var statement in block.DescendantNodes().OfType<ThrowStatementSyntax>())
        {
            if (statement.Expression is null)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CatchBlockLogs(BlockSyntax block)
    {
        foreach (var invocation in block.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var name = invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                IdentifierNameSyntax id => id.Identifier.ValueText,
                _ => ""
            };
            if (name is "LogWarning" or "LogError" or "LogCritical" or "LogInformation" or "LogDebug")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Names of <c>[Inject]</c> properties, constructor / primary-constructor parameters, and
    /// <c>@inject</c> members typed as <c>HttpClient</c>.
    /// </summary>
    internal static IReadOnlyList<string> HttpClientInjections(string source)
    {
        var names = new List<string>();
        var root = ArchitectureRules.Parse(source);
        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (HasInjectAttribute(property) && IsHttpClientType(UnwrapNullable(property.Type)))
            {
                names.Add(property.Identifier.ValueText);
            }
        }

        foreach (var parameter in root.DescendantNodes().OfType<ParameterSyntax>())
        {
            if (parameter.Type != null &&
                IsConstructorOrTypeParameter(parameter) &&
                IsHttpClientType(UnwrapNullable(parameter.Type)))
            {
                names.Add(parameter.Identifier.ValueText);
            }
        }

        foreach (var line in source.Split('\n'))
        {
            CollectRazorHttpClientInject(line.Trim(), names);
        }

        return Sorted(names);
    }

    /// <summary>
    /// Type names from <c>new HttpClient</c> (or qualified) and target-typed <c>HttpClient c = new();</c>.
    /// </summary>
    internal static IReadOnlyList<string> HttpClientConstructions(string source)
    {
        var names = new List<string>();
        var root = ArchitectureRules.Parse(source);
        foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (IsHttpClientType(creation.Type))
            {
                names.Add("HttpClient");
            }
        }

        foreach (var creation in root.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>())
        {
            if (IsHttpClientTargetTypedCreation(creation))
            {
                names.Add("HttpClient");
            }
        }

        return Sorted(names);
    }

    private static bool IsHttpClientTargetTypedCreation(ImplicitObjectCreationExpressionSyntax creation)
    {
        if (creation.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator })
        {
            return false;
        }

        return declarator.Parent is VariableDeclarationSyntax declaration &&
               IsHttpClientType(UnwrapNullable(declaration.Type));
    }

    private static void CollectRazorHttpClientInject(string trimmed, List<string> names)
    {
        if (!trimmed.StartsWith("@inject ", StringComparison.Ordinal))
        {
            return;
        }

        var parts = trimmed["@inject ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return;
        }

        var typeName = parts[0].Split('.').Last();
        if (typeName == "HttpClient")
        {
            names.Add(parts[1]);
        }
    }

    private static bool IsHttpClientType(TypeSyntax type) =>
        SimpleTypeName(type) == "HttpClient";

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
