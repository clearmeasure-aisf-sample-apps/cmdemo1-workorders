using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.BuildGates;
using FluentValidation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

/// <summary>
/// Deterministic gates for the single MediatR validation pipeline (#1804): no empty
/// <see cref="AbstractValidator{T}"/> markers, HTTP allow-list is remotable markers, and
/// production code does not <c>new</c> validators outside the pipeline.
/// </summary>
[TestFixture]
public class ValidationPipelineArchitectureTests
{
    [Test]
    public void ShouldHaveNoEmptyAbstractValidators_WhenProductionSourcesAreScanned()
    {
        var empty = EmptyAbstractValidators(RepositoryCheckout.Current);

        empty.ShouldBeEmpty(
            "Empty AbstractValidator<T> classes (marker-only): " + string.Join(", ", empty));
    }

    [Test]
    public void ShouldAllowOnlyRemotableMarkers_WhenHttpPayloadAllowListIsRead()
    {
        var source = RepositoryCheckout.Current.ReadText(
            "src/UI/Server/Middleware/WebServiceMessagePayloadValidator.cs");

        source.ShouldContain(nameof(IRemotableRequest));
        source.ShouldContain(nameof(IRemotableEvent));
        source.ShouldNotContain("No validator registered");
        source.ShouldNotContain("GetMethod");
        source.ShouldNotContain("GetProperty");
    }

    [Test]
    public void ShouldNotNewValidatorsOutsideValidationBehavior_WhenProductionSourcesAreScanned()
    {
        var offenders = ProductionValidatorNews(RepositoryCheckout.Current);

        offenders.ShouldBeEmpty(
            "Production new *Validator outside ValidationBehavior: " + string.Join(", ", offenders));
    }

    [Test]
    public void ShouldResideInUiShared_WhenValidationBehaviorIsThePipeline()
    {
        typeof(ValidationBehavior<,>).Namespace.ShouldBe("ClearMeasure.Bootcamp.UI.Shared");
        typeof(ValidationBehavior<,>).Assembly.ShouldBe(typeof(Bus).Assembly);
    }

    internal static IReadOnlyList<string> EmptyAbstractValidators(RepositoryCheckout checkout)
    {
        var empty = new List<string>();
        foreach (var file in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(file);
            if (relative.StartsWith("src/UnitTests/", StringComparison.Ordinal)
                || relative.StartsWith("src/IntegrationTests/", StringComparison.Ordinal)
                || relative.StartsWith("src/AcceptanceTests/", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var name in EmptyAbstractValidatorNames(File.ReadAllText(file)))
            {
                empty.Add(relative + ":" + name);
            }
        }

        return empty;
    }

    internal static IReadOnlyList<string> EmptyAbstractValidatorNames(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        var names = new List<string>();
        foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (!DerivesFromAbstractValidator(type) || !HasEmptyValidatorBody(type))
            {
                continue;
            }

            names.Add(type.Identifier.ValueText);
        }

        return names;
    }

    internal static IReadOnlyList<string> ProductionValidatorNews(RepositoryCheckout checkout)
    {
        var offenders = new List<string>();
        foreach (var file in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(file);
            if (!IsProductionSource(relative) || relative.EndsWith("/ValidationBehavior.cs", StringComparison.Ordinal))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                var typeName = creation.Type.ToString();
                if (typeName.EndsWith("Validator", StringComparison.Ordinal)
                    && !typeName.Contains("ChatClientConfigValidator", StringComparison.Ordinal))
                {
                    offenders.Add($"{relative}:{creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
                }
            }
        }

        return offenders;
    }

    private static bool IsProductionSource(string relative) =>
        relative.StartsWith("src/", StringComparison.Ordinal)
        && !relative.StartsWith("src/UnitTests/", StringComparison.Ordinal)
        && !relative.StartsWith("src/IntegrationTests/", StringComparison.Ordinal)
        && !relative.StartsWith("src/AcceptanceTests/", StringComparison.Ordinal);

    private static bool DerivesFromAbstractValidator(TypeDeclarationSyntax type)
    {
        if (type.BaseList is null)
        {
            return false;
        }

        foreach (var baseType in type.BaseList.Types)
        {
            if (baseType.Type is GenericNameSyntax { Identifier.ValueText: "AbstractValidator" })
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasEmptyValidatorBody(TypeDeclarationSyntax type)
    {
        if (type.ParameterList is not null && type.SemicolonToken.IsKind(SyntaxKind.SemicolonToken))
        {
            return true;
        }

        if (type.SemicolonToken.IsKind(SyntaxKind.SemicolonToken) && type.Members.Count == 0)
        {
            return true;
        }

        return type.Members.Count == 0
               && !type.OpenBraceToken.IsKind(SyntaxKind.None)
               && !type.CloseBraceToken.IsKind(SyntaxKind.None);
    }
}
