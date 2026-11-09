using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

/// <summary>
/// Parses solution C# into merged type declarations. Test doubles count as implementations.
/// </summary>
internal static class SourceTypeParser
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview);

    public static IReadOnlyList<ParsedType> ParseRepository(RepositoryCheckout checkout)
    {
        var contexts = checkout.SourceFiles(".cs").Select(path => ParseFile(checkout, path)).ToList();
        ApplyGlobalUsings(contexts);
        return MergePartials(contexts.SelectMany(context => context.Sink));
    }

    private static ParseContext ParseFile(RepositoryCheckout checkout, string path)
    {
        var text = File.ReadAllText(path);
        var root = CSharpSyntaxTree.ParseText(text, ParseOptions).GetCompilationUnitRoot();
        var context = new ParseContext
        {
            ProjectDirectory = ProjectDirectory(checkout, path),
            RelativePath = checkout.Relative(path)
        };
        ReadUsings(root.Usings, context);
        WalkMembers(root.Members, context);
        return context;
    }

    private static void ReadUsings(SyntaxList<UsingDirectiveSyntax> usings, ParseContext context)
    {
        foreach (var directive in usings)
        {
            var targetUsings = directive.GlobalKeyword.IsKind(SyntaxKind.None) ? context.Usings : context.GlobalUsings;
            var targetAliases = directive.GlobalKeyword.IsKind(SyntaxKind.None) ? context.Aliases : context.GlobalAliases;
            ReadUsing(directive, targetUsings, targetAliases);
        }
    }

    private static void ReadUsing(UsingDirectiveSyntax directive, List<string> usings, List<UsingAlias> aliases)
    {
        if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
        {
            return;
        }

        if (directive.Alias != null)
        {
            ReadAlias(directive, aliases);
            return;
        }

        usings.Add(directive.NamespaceOrType.ToString());
    }

    private static void ReadAlias(UsingDirectiveSyntax directive, List<UsingAlias> aliases)
    {
        if (directive.Alias == null
            || !TypeNameSyntax.TryFormat(directive.NamespaceOrType, out var name, out var arity))
        {
            return;
        }

        aliases.Add(new UsingAlias(directive.Alias.Name.ToString(), name, arity));
    }

    private static void WalkMembers(SyntaxList<MemberDeclarationSyntax> members, ParseContext context)
    {
        foreach (var member in members)
        {
            if (member is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                WalkNamespace(namespaceDeclaration, context);
            }
            else if (member is TypeDeclarationSyntax type)
            {
                CollectType(type, context);
            }
        }
    }

    private static void WalkNamespace(BaseNamespaceDeclarationSyntax declaration, ParseContext context)
    {
        var name = declaration.Name.ToString();
        var fullName = context.Namespace.Length == 0 ? name : context.Namespace + "." + name;
        var nested = context.WithNamespace(fullName, new List<string>(context.Usings), new List<UsingAlias>(context.Aliases));
        ReadUsings(declaration.Usings, nested);
        WalkMembers(declaration.Members, nested);
    }

    private static void CollectType(TypeDeclarationSyntax type, ParseContext context)
    {
        var names = TypeNamesOf(type, context);
        context.Sink.Add(Describe(type, context, names));
        WalkMembers(type.Members, context.WithParent(names.Plus, names.Dotted));
    }

    private static TypeNames TypeNamesOf(TypeDeclarationSyntax type, ParseContext context)
    {
        var simpleName = type.Identifier.ValueText;
        var arity = type.TypeParameterList?.Parameters.Count ?? 0;
        return new TypeNames(Join(context.ParentPlus, simpleName, "+"), Join(context.ParentDots, simpleName, "."), arity);
    }

    private static string Join(string parent, string name, string separator)
    {
        return parent.Length == 0 ? name : parent + separator + name;
    }

    private static ParsedType Describe(TypeDeclarationSyntax type, ParseContext context, TypeNames names)
    {
        var fileLocal = HasModifier(type.Modifiers, SyntaxKind.FileKeyword);
        var facts = type is InterfaceDeclarationSyntax ? PassThroughFacts.None : PassThroughSyntax.Inspect(type);
        return new ParsedType
        {
            Id = TypeId(context, names.Plus, names.Arity, fileLocal),
            LookupName = names.Dotted,
            Arity = names.Arity,
            Namespace = context.Namespace,
            Kind = KindOf(type),
            IsAbstract = HasModifier(type.Modifiers, SyntaxKind.AbstractKeyword),
            IsStatic = HasModifier(type.Modifiers, SyntaxKind.StaticKeyword),
            ProjectDirectory = context.ProjectDirectory,
            Usings = new List<string>(context.Usings),
            Aliases = new List<UsingAlias>(context.Aliases),
            Bases = ReadBases(type),
            SawForwarder = facts.SawForwarder,
            PassThroughDisqualified = facts.Disqualified,
            ForwardTarget = facts.Target,
        };
    }

    private readonly record struct TypeNames(string Plus, string Dotted, int Arity);

    private static string TypeId(ParseContext context, string plusName, int arity, bool fileLocal)
    {
        var qualified = context.Namespace.Length == 0 ? plusName : context.Namespace + "." + plusName;
        if (arity > 0)
        {
            qualified += "`" + arity;
        }

        return fileLocal ? qualified + "@" + context.RelativePath : qualified;
    }

    private static List<BaseTypeRef> ReadBases(TypeDeclarationSyntax type)
    {
        var bases = new List<BaseTypeRef>();
        if (type.BaseList == null)
        {
            return bases;
        }

        foreach (var baseType in type.BaseList.Types)
        {
            if (TypeNameSyntax.TryFormat(baseType.Type, out var name, out var arity))
            {
                bases.Add(new BaseTypeRef(name, arity));
            }
        }

        return bases;
    }

    private static DeclaredTypeKind KindOf(TypeDeclarationSyntax type)
    {
        if (type is InterfaceDeclarationSyntax)
        {
            return DeclaredTypeKind.Interface;
        }

        if (type is StructDeclarationSyntax)
        {
            return DeclaredTypeKind.Struct;
        }

        if (type is RecordDeclarationSyntax record
            && record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
        {
            return DeclaredTypeKind.Struct;
        }

        if (type is RecordDeclarationSyntax)
        {
            return DeclaredTypeKind.Record;
        }

        return DeclaredTypeKind.Class;
    }

    private static void ApplyGlobalUsings(IReadOnlyList<ParseContext> files)
    {
        var byProject = files.GroupBy(file => file.ProjectDirectory);
        foreach (var project in byProject)
        {
            var usings = project.SelectMany(file => file.GlobalUsings).Distinct(StringComparer.Ordinal).ToList();
            var aliases = project.SelectMany(file => file.GlobalAliases).Distinct().ToList();
            foreach (var type in project.SelectMany(file => file.Sink))
            {
                type.Usings.AddRange(usings.Where(name => !type.Usings.Contains(name, StringComparer.Ordinal)));
                type.Aliases.AddRange(aliases.Where(alias => !type.Aliases.Contains(alias)));
            }
        }
    }

    private static IReadOnlyList<ParsedType> MergePartials(IEnumerable<ParsedType> types)
    {
        return types
            .GroupBy(type => type.ProjectDirectory + "|" + type.Id)
            .Select(MergeGroup)
            .OrderBy(type => type.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static ParsedType MergeGroup(IGrouping<string, ParsedType> group)
    {
        var parts = group.ToList();
        var first = parts[0];
        var targets = parts.Where(part => part.SawForwarder).Select(part => part.ForwardTarget).Distinct().ToList();
        var disqualified = parts.Any(part => part.PassThroughDisqualified) || targets.Count > 1;
        var sawForwarder = !disqualified && targets.Count == 1;
        return new ParsedType
        {
            Id = first.Id,
            LookupName = first.LookupName,
            Arity = first.Arity,
            Namespace = first.Namespace,
            Kind = first.Kind,
            IsAbstract = parts.Any(part => part.IsAbstract),
            IsStatic = parts.Any(part => part.IsStatic),
            ProjectDirectory = first.ProjectDirectory,
            Usings = parts.SelectMany(part => part.Usings).Distinct(StringComparer.Ordinal).ToList(),
            Aliases = parts.SelectMany(part => part.Aliases).Distinct().ToList(),
            Bases = parts.SelectMany(part => part.Bases).Distinct().ToList(),
            SawForwarder = sawForwarder,
            PassThroughDisqualified = disqualified,
            ForwardTarget = sawForwarder ? targets[0] : null
        };
    }

    private static string ProjectDirectory(RepositoryCheckout checkout, string filePath)
    {
        var src = checkout.PathOf("src");
        var directory = Path.GetDirectoryName(filePath) ?? src;
        while (IsUnder(directory, src))
        {
            if (Directory.EnumerateFiles(directory, "*.csproj").Any())
            {
                return directory;
            }

            var parent = Path.GetDirectoryName(directory);
            if (string.IsNullOrEmpty(parent) || parent == directory)
            {
                break;
            }

            directory = parent;
        }

        return "";
    }

    private static bool IsUnder(string directory, string root)
    {
        return string.Equals(directory, root, StringComparison.Ordinal)
            || directory.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static bool HasModifier(SyntaxTokenList modifiers, SyntaxKind kind)
    {
        return modifiers.Any(token => token.IsKind(kind));
    }
}
