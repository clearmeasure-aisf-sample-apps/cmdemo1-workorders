namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// One layer of the Onion Architecture: a solution project, the namespace its types live in,
/// and the layers it may depend on. The allowed set mirrors the project references in <c>src/</c>.
/// </summary>
public sealed record OnionLayer(string Name, string RootNamespace, IReadOnlyList<string> MayDependOn)
{
    /// <summary>Regular expression for <see cref="RootNamespace"/> and every namespace below it.</summary>
    public string NamespacePattern => $@"^{System.Text.RegularExpressions.Regex.Escape(RootNamespace)}(\..+)?$";

    /// <summary>The layer's name, so NUnit test-case names read as layers.</summary>
    public override string ToString()
    {
        return Name;
    }
}
