using ClearMeasure.NpoAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Analyzers;

[TestFixture]
public class LoopHelpersTests
{
    [TestCase("for (var i = 0; i < 3; i++) { Target(); }")]
    [TestCase("for (var i = 0; i < 3; i++) Target();")]
    [TestCase("for (var i = 0; i < Target(); i++) { }")]
    [TestCase("for (var i = 0; i < 3; i += Target()) { }")]
    [TestCase("for (var i = 0; i < 3; i++, Target()) { }")]
    [TestCase("while (Target() > 0) { }")]
    [TestCase("while (true) { Target(); }")]
    [TestCase("do { } while (Target() > 0);")]
    [TestCase("do { Target(); } while (true);")]
    [TestCase("foreach (var item in items) { Target(); }")]
    [TestCase("foreach (var (a, b) in pairs) { Target(); }")]
    [TestCase("var query = items.Select(item => Target());")]
    [TestCase("var query = items.SelectMany(item => Target());")]
    [TestCase("var query = items.SelectAwait(item => Target());")]
    [TestCase("items.ForEach((item) => Target());")]
    [TestCase("items.ForEachAsync(item => Target());")]
    [TestCase("var query = items.Select(delegate (int item) { return Target(); });")]
    [TestCase("var query = items?.Select(item => Target());")]
    [TestCase("for (var i = 0; i < 3; i++) { int Local() => Target(); }")]
    [TestCase("while (true) { int Local() { return Target(); } }")]
    [TestCase("if (flag) { for (;;) { if (other) { Target(); } } }")]
    public void IsInsideLoop_WhenCallRepeatsPerIteration_ShouldReturnTrue(string statements)
    {
        var invocation = FindTarget(statements);

        LoopHelpers.IsInsideLoop(invocation).ShouldBeTrue();
    }

    [TestCase("Target();")]
    [TestCase("for (var i = Target(); i < 3; i++) { }")]
    [TestCase("for (Target(); ; ) { break; }")]
    [TestCase("foreach (var item in Target()) { }")]
    [TestCase("foreach (var (a, b) in Target()) { }")]
    [TestCase("var query = items.Where(item => Target() > 0);")]
    [TestCase("var query = Select(Target());")]
    [TestCase("int Local() => Target();")]
    [TestCase("if (flag) { Target(); }")]
    public void IsInsideLoop_WhenCallRunsOnce_ShouldReturnFalse(string statements)
    {
        var invocation = FindTarget(statements);

        LoopHelpers.IsInsideLoop(invocation).ShouldBeFalse();
    }

    [Test]
    public void IsInsideLoop_WhenCallIsInForeachCollectionInsideOuterLoop_ShouldReturnTrue()
    {
        var invocation = FindTarget("while (true) { foreach (var item in Target()) { } }");

        LoopHelpers.IsInsideLoop(invocation).ShouldBeTrue();
    }

    [TestCase("client.Target()", "Target")]
    [TestCase("client?.Target()", "Target")]
    [TestCase("Target()", "Target")]
    [TestCase("Target<int>()", "Target")]
    [TestCase("client.Target<int>()", "Target")]
    [TestCase("client?.Inner.Target()", "Target")]
    [TestCase("this.Target()", "Target")]
    public void GetInvocationName_WhenExpressionIsNamed_ShouldReturnMethodName(string expression, string expected)
    {
        var invocation = ParseInvocation(expression);

        LoopHelpers.GetInvocationName(invocation).ShouldBe(expected);
    }

    [TestCase("((System.Func<int>)factory)()")]
    [TestCase("factory()()")]
    [TestCase("handlers[0]()")]
    public void GetInvocationName_WhenExpressionHasNoName_ShouldReturnNull(string expression)
    {
        var invocation = ParseInvocation(expression);

        LoopHelpers.GetInvocationName(invocation).ShouldBeNull();
    }

    [TestCase("await Target()")]
    [TestCase("await client.Target()")]
    [TestCase("await Target().ConfigureAwait(false)")]
    [TestCase("await (Target())")]
    [TestCase("await ((Target()))")]
    [TestCase("await (Target().ConfigureAwait(false))")]
    [TestCase("await (Target()).ConfigureAwait(false)")]
    [TestCase("await Target().ConfigureAwait(false).ConfigureAwait(true)")]
    public void GetAwaitedInvocation_WhenAwaitWrapsCall_ShouldReturnUnderlyingCall(string expression)
    {
        var awaitExpression = ParseAwait(expression);

        var invocation = LoopHelpers.GetAwaitedInvocation(awaitExpression);

        invocation.ShouldNotBeNull();
        LoopHelpers.GetInvocationName(invocation).ShouldBe("Target");
    }

    [TestCase("await pending")]
    [TestCase("await (pending)")]
    [TestCase("await pending.Result")]
    public void GetAwaitedInvocation_WhenAwaitHasNoCall_ShouldReturnNull(string expression)
    {
        var awaitExpression = ParseAwait(expression);

        LoopHelpers.GetAwaitedInvocation(awaitExpression).ShouldBeNull();
    }

    [Test]
    public void GetAwaitedInvocation_WhenConfigureAwaitIsUnqualified_ShouldReturnConfigureAwaitCall()
    {
        var awaitExpression = ParseAwait("await ConfigureAwait(false)");

        var invocation = LoopHelpers.GetAwaitedInvocation(awaitExpression);

        invocation.ShouldNotBeNull();
        LoopHelpers.GetInvocationName(invocation).ShouldBe("ConfigureAwait");
    }

    private static InvocationExpressionSyntax FindTarget(string statements)
    {
        var root = ParseMethodBody(statements);
        return root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(IsTargetCall);
    }

    private static bool IsTargetCall(InvocationExpressionSyntax invocation) =>
        invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "Target" };

    private static InvocationExpressionSyntax ParseInvocation(string expression)
    {
        var root = ParseMethodBody($"var result = {expression};");
        return root.DescendantNodes().OfType<InvocationExpressionSyntax>().First();
    }

    private static AwaitExpressionSyntax ParseAwait(string expression)
    {
        var root = ParseMethodBody($"var result = {expression};");
        return root.DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
    }

    private static SyntaxNode ParseMethodBody(string statements)
    {
        var source = $$"""
            public class Sample
            {
                public async System.Threading.Tasks.Task Run()
                {
                    {{statements}}
                }
            }
            """;
        var tree = CSharpSyntaxTree.ParseText(source);
        tree.GetDiagnostics().ShouldBeEmpty();
        return tree.GetRoot();
    }
}
