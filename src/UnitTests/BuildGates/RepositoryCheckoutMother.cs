namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// Paths and trees for the <see cref="RepositoryCheckout"/> fixture.
/// </summary>
internal static class RepositoryCheckoutMother
{
    public static readonly string[] HandWrittenSources =
    [
        "src/Core/Model/WorkOrder.cs",
        "src/UI.Shared/Pages/Index.razor",
        "src/UI.Shared/Pages/Index.razor.cs",
    ];

    public static readonly string[] NotHandWritten =
    [
        "src/UI/Server/Generated/Protos/Workorders.cs",
        "src/UnitTests/bin/Debug/Foo.cs",
        "src/Core/obj/Debug/Foo.cs",
        "src/UnitTests/TestResults/Foo.cs",
        "src/UI/Client/node_modules/lib/Foo.cs",
        "src/UI/Server/Foo.g.cs",
        "src/UI/Server/Foo.Designer.cs",
    ];

    public static ScratchTree TreeWithEveryKindOfFile()
    {
        var tree = new ScratchTree("checkout-sources");
        foreach (var path in HandWrittenSources.Concat(NotHandWritten))
        {
            tree.Write(path, "// source");
        }

        return tree.Write("src/Core/Core.csproj", "<Project />").Write("README.md", "# Readme");
    }
}
