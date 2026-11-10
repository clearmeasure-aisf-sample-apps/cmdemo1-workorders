using System.Text.RegularExpressions;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags type names that show no concept was found: a vague word (<c>Extra</c>, <c>More</c>, <c>Misc</c>), a
/// numbered part (<c>Part1</c>) or a trailing number (<c>Manager2</c>). Technical names that end in a number
/// (<c>Sha256</c>, <c>Base64</c>) are not numbered names.
/// </summary>
public sealed partial class BannedTypeNameRule : IStructureRule
{
    private static readonly string[] VagueWords = ["Extra", "More", "Misc", "Part"];

    private static readonly string[] TechnicalNumberedWords =
    [
        "Base64", "Sha1", "Sha256", "Sha384", "Sha512", "Md5", "Http2", "Http3", "Utf8", "Utf16", "Bcp47",
        "Iso8601", "Crc32", "Int16", "Int32", "Int64", "X509", "Ipv4", "Ipv6", "OAuth2"
    ];

    public string BaselineFileName => "banned-type-names.txt";

    public string Guidance =>
        "A numbered or vague name means the abstraction has not been named yet. Name the type for the domain " +
        "concept it represents (ubiquitous language).";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files.SelectMany(file => file.TypeDeclarations
            .Where(type => IsBanned(type.Identifier.Text))
            .Select(file.ViolationAt));

    private static bool IsBanned(string typeName)
    {
        var words = Word().Matches(typeName).Select(match => match.Value).ToArray();
        if (words.Any(word => VagueWords.Contains(word, StringComparer.Ordinal)))
        {
            return true;
        }

        var trailingNumber = TrailingNumber().Match(typeName);
        return trailingNumber.Success
               && !TechnicalNumberedWords.Any(word => typeName.EndsWith(word, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z][a-z]*|[a-z]+|[0-9]+")]
    private static partial Regex Word();

    [GeneratedRegex("[0-9]+$")]
    private static partial Regex TrailingNumber();
}
