namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

/// <summary>
/// A console project whose only diagnostic is a <c>#warning</c>, for proving which builds treat warnings as errors.
/// </summary>
internal static class CompilerWarningProjectMother
{
    public const string WarningId = "CS1030";

    private const string Project = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <OutputType>Exe</OutputType>
          </PropertyGroup>
        </Project>
        """;

    private const string Program = """
        #warning Warning gate contract
        System.Console.WriteLine("warning gate");
        """;

    public static ScratchTree ProgramWithAWarningDirective()
    {
        return new ScratchTree("compiler-warning-gate")
            .Write("WarningGate.csproj", Project)
            .Write("Program.cs", Program);
    }
}
