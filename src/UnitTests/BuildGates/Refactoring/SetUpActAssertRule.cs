using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates.Refactoring;

/// <summary>
/// Flags a <c>[SetUp]</c> or <c>[OneTimeSetUp]</c> that asserts, or that invokes the subject under test when the
/// fixture name does not start with <c>Given</c>. The subject under test is the fixture name without its
/// <c>Tests</c> suffix: a variable declared as or constructed with that type, or <c>Render&lt;Subject&gt;()</c>.
/// </summary>
public sealed class SetUpActAssertRule : IStructureRule
{
    public string BaselineFileName => "setup-act-assert.txt";

    public string Guidance =>
        "[SetUp] does infrastructure. Shared arrange steps belong in setup only when the fixture name says so " +
        "(Given...); act and assert always belong in the test method.";

    public IEnumerable<StructureViolation> FindViolations(SourceSet sources) =>
        sources.Files
            .Where(file => file.IsTest)
            .SelectMany(file => file.Classes.SelectMany(fixture => NUnitSyntax.SetUps(fixture)
                .Where(setUp => NUnitSyntax.ContainsAssertion(setUp) || ActsOnSubject(fixture, setUp))
                .Select(file.ViolationAt)));

    private static bool ActsOnSubject(ClassDeclarationSyntax fixture, MethodDeclarationSyntax setUp)
    {
        var fixtureName = fixture.Identifier.Text;
        var subject = SubjectName(fixtureName);
        if (fixtureName.StartsWith("Given", StringComparison.Ordinal) || subject.Length == 0)
        {
            return false;
        }

        var subjectVariables = SubjectVariables(fixture, subject);
        return setUp.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
            IsRenderOf(invocation, subject) || IsCallOn(invocation, subject, subjectVariables));
    }

    private static string SubjectName(string fixtureName) =>
        fixtureName.EndsWith("Tests", StringComparison.Ordinal) ? fixtureName[..^"Tests".Length] : "";

    private static HashSet<string> SubjectVariables(ClassDeclarationSyntax fixture, string subject)
    {
        var fromDeclarations = fixture.DescendantNodes()
            .OfType<VariableDeclarationSyntax>()
            .SelectMany(declaration => declaration.Variables
                .Where(variable => IsSubjectType(declaration.Type, subject) || ConstructsSubject(variable, subject))
                .Select(variable => variable.Identifier.Text));
        var fromProperties = fixture.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => IsSubjectType(property.Type, subject))
            .Select(property => property.Identifier.Text);
        return fromDeclarations.Concat(fromProperties).ToHashSet(StringComparer.Ordinal);
    }

    private static bool IsSubjectType(TypeSyntax type, string subject) =>
        type is IdentifierNameSyntax identifier && identifier.Identifier.Text == subject
        || type is NullableTypeSyntax { ElementType: IdentifierNameSyntax element } && element.Identifier.Text == subject;

    private static bool ConstructsSubject(VariableDeclaratorSyntax variable, string subject) =>
        variable.Initializer?.Value is ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax created }
        && created.Identifier.Text == subject;

    private static bool IsRenderOf(InvocationExpressionSyntax invocation, string subject)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name,
            SimpleNameSyntax simple => simple,
            _ => null
        };
        return name is GenericNameSyntax generic
               && generic.Identifier.Text.StartsWith("Render", StringComparison.Ordinal)
               && generic.TypeArgumentList.Arguments.Any(argument => argument.ToString() == subject);
    }

    private static bool IsCallOn(InvocationExpressionSyntax invocation, string subject, HashSet<string> variables) =>
        invocation.Expression is MemberAccessExpressionSyntax member
        && member.Expression switch
        {
            IdentifierNameSyntax receiver => variables.Contains(receiver.Identifier.Text),
            ObjectCreationExpressionSyntax { Type: IdentifierNameSyntax created } => created.Identifier.Text == subject,
            _ => false
        };
}
