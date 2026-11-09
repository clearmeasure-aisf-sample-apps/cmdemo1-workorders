namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Ratchet keys and baseline files for the <see cref="KeyRatchet"/> and <see cref="RatchetComparison"/> fixtures.
/// </summary>
internal static class RatchetKeysMother
{
    public const string WideFile = "file src/Core/A.cs";

    public const string LongMethod = "method src/Core/A.cs A.Run()";

    public const string WideParameters = "parameters src/Core/B.cs B(int,int,int,int,int)";

    public static KeyRatchet Listing(params string[] keys)
    {
        return KeyRatchet.Parse(string.Join('\n', keys));
    }

    public static string CommentedBaselineText()
    {
        return "\uFEFF# grandfathered violations\r\n\r\n  " + WideFile + "  \r\n# end\r\n" + LongMethod;
    }

    public static string[] Numbered(int count)
    {
        return Enumerable.Range(1, count).Select(number => $"file src/Core/File{number:D2}.cs").ToArray();
    }
}
