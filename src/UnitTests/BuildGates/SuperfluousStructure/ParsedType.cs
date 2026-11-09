namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.SuperfluousStructure;

internal enum DeclaredTypeKind
{
    Interface,
    Class,
    Record,
    Struct
}

internal sealed class ParsedType
{
    public string Id { get; init; } = "";

    public string LookupName { get; init; } = "";

    public int Arity { get; init; }

    public string Namespace { get; init; } = "";

    public DeclaredTypeKind Kind { get; init; }

    public bool IsAbstract { get; init; }

    public bool IsStatic { get; init; }

    public string ProjectDirectory { get; init; } = "";

    public List<string> Usings { get; init; } = [];

    public List<UsingAlias> Aliases { get; init; } = [];

    public List<BaseTypeRef> Bases { get; init; } = [];

    public bool SawForwarder { get; init; }

    public bool PassThroughDisqualified { get; init; }

    public string? ForwardTarget { get; init; }
}

internal readonly record struct BaseTypeRef(string Name, int Arity);

internal readonly record struct UsingAlias(string Alias, string Name, int Arity);
