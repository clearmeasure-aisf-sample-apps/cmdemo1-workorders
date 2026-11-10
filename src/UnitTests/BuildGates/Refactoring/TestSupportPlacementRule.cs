using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Places test data and test actions: an <c>ObjectMother</c>, <c>*Mother</c>, <c>*Builder</c> or <c>*Steps</c>
/// type in a test project is a top-level type in its own file named after it, next to no fixture, and asserts
/// nothing (assertions stay visible in the test method).
/// </summary>
public sealed class TestSupportPlacementRule : IStructureRule
{
    private static readonly string[] SupportSuffixes = ["Mother", "Builder", "Steps"];

    public string BaselineFileName => "test-support-placement.txt";

    public string Guidance =>
        "Test data lives in an ObjectMother or builder, actions in a small domain step class (WorkOrderSteps): " +
        "each in its own file named after it, with no fixture beside it and no assertions inside it.";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files
            .Where(file => file.IsTest)
            .SelectMany(file => file.TypeDeclarations
                .OfType<TypeDeclarationSyntax>()
                .Where(IsTestSupport)
                .Where(support => !IsPlacedWell(file, support))
                .Select(file.ViolationAt));

    private static bool IsTestSupport(TypeDeclarationSyntax type) =>
        SupportSuffixes.Any(suffix => type.Identifier.Text.EndsWith(suffix, StringComparison.Ordinal));

    private static bool IsPlacedWell(SourceFile file, TypeDeclarationSyntax support) =>
        support.Parent is not TypeDeclarationSyntax
        && Path.GetFileNameWithoutExtension(file.RelativePath) == support.Identifier.Text
        && !file.Classes.Any(NUnitSyntax.IsFixture)
        && !NUnitSyntax.ContainsAssertion(support);
}
