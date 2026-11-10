using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags a test whose only statement calls a helper and asserts nothing itself, the shape of a whole-scenario
/// helper such as <c>RunAssignmentScenario()</c>. Every step of the test stays visible in the test method.
/// </summary>
public sealed class WholeScenarioTestRule : IStructureRule
{
    public string BaselineFileName => "whole-scenario-tests.txt";

    public string Guidance =>
        "A single helper that runs the whole scenario hides every step. Show each arrange, act and assert step in " +
        "the test as one call to a well-named step, builder or ObjectMother method.";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files
            .Where(file => file.IsTest)
            .SelectMany(file => NUnitSyntax.Tests(file)
                .Where(test => StatementCount(test) <= 1 && !NUnitSyntax.ContainsAssertion(test))
                .Select(file.ViolationAt));

    private static int StatementCount(MethodDeclarationSyntax test) => test.Body?.Statements.Count ?? 1;
}
