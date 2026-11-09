namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

internal sealed class ParseContext
{
    public string Namespace { get; init; } = "";

    public string ParentPlus { get; init; } = "";

    public string ParentDots { get; init; } = "";

    public List<string> Usings { get; init; } = [];

    public List<UsingAlias> Aliases { get; init; } = [];

    public string ProjectDirectory { get; init; } = "";

    public string RelativePath { get; init; } = "";

    public List<ParsedType> Sink { get; init; } = [];

    public List<string> GlobalUsings { get; init; } = [];

    public List<UsingAlias> GlobalAliases { get; init; } = [];

    public ParseContext WithNamespace(string namespaceName, List<string> usings, List<UsingAlias> aliases)
    {
        return new ParseContext
        {
            Namespace = namespaceName,
            Usings = usings,
            Aliases = aliases,
            ProjectDirectory = ProjectDirectory,
            RelativePath = RelativePath,
            Sink = Sink,
            GlobalUsings = GlobalUsings,
            GlobalAliases = GlobalAliases
        };
    }

    public ParseContext WithParent(string parentPlus, string parentDots)
    {
        return new ParseContext
        {
            Namespace = Namespace,
            ParentPlus = parentPlus,
            ParentDots = parentDots,
            Usings = Usings,
            Aliases = Aliases,
            ProjectDirectory = ProjectDirectory,
            RelativePath = RelativePath,
            Sink = Sink,
            GlobalUsings = GlobalUsings,
            GlobalAliases = GlobalAliases
        };
    }
}
