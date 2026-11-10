using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags loops and branches (<c>if</c>, <c>switch</c>, <c>?:</c>, <c>for</c>, <c>foreach</c>, <c>while</c>,
/// <c>do</c>) in a test method. A test reads as a straight list of steps.
/// </summary>
public sealed class TestControlFlowRule : IStructureRule
{
    public string BaselineFileName => "test-control-flow.txt";

    public string Guidance =>
        "A test method reads procedurally: no loops or branches. Use [TestCase]/[TestCaseSource] for several inputs " +
        "and move iteration into a well-named step or builder.";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files
            .Where(file => file.IsTest)
            .SelectMany(file => NUnitSyntax.Tests(file)
                .Where(HasControlFlow)
                .Select(file.ViolationAt));

    private static bool HasControlFlow(MethodDeclarationSyntax test) =>
        test.DescendantNodes().Any(node => node is IfStatementSyntax
            or SwitchStatementSyntax
            or SwitchExpressionSyntax
            or ConditionalExpressionSyntax
            or ForStatementSyntax
            or ForEachStatementSyntax
            or WhileStatementSyntax
            or DoStatementSyntax);
}
