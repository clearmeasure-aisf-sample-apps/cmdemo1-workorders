using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.LlmGateway;
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
/// <see cref="AbstractValidator{T}"/> markers, HTTP allow-list is remotable markers matching
/// master's set (LLM config/chat queries are not remotable), and production code does not
/// <c>new</c> a FluentValidation <see cref="AbstractValidator{T}"/> outside the pipeline.
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
    public void ShouldKeepLlmQueriesServerOnly_WhenHttpAllowListMatchesMaster()
    {
        typeof(IRemotableRequest).IsAssignableFrom(typeof(ChatClientConfigQuery)).ShouldBeFalse();
        typeof(IRemotableRequest).IsAssignableFrom(typeof(WorkOrderChatQuery)).ShouldBeFalse();
    }

    [Test]
    public void ShouldMatchMasterAllowList_WhenConcreteRemotableTypesAreListed()
    {
        HttpAllowListTypes.ConcreteRemotableTypeNames().ShouldBe(HttpAllowListTypes.MasterNames);
    }

    [Test]
    public void ShouldNotNewAbstractValidatorsOutsideValidationBehavior_WhenProductionSourcesAreScanned()
    {
        var offenders = ProductionValidatorNews(RepositoryCheckout.Current);

        offenders.ShouldBeEmpty(
            "Production new AbstractValidator outside ValidationBehavior: "
            + string.Join(", ", offenders));
    }

    [Test]
    public void ShouldAllowOnlyNamedSites_WhenProductionConstructsValidatorsViaActivator()
    {
        var sites = ProductionValidatorActivatorSites(RepositoryCheckout.Current);

        sites.ShouldBe(
            AllowedValidatorActivatorSites,
            "Only HttpEnvelopeValidatorSet (HTTP) and InProcessCreateCommandShapeValidation "
            + "(#1824 in-process Failed results) may construct deferred validators via Activator. "
            + "Found: " + string.Join(", ", sites));
    }

    [Test]
    public void ShouldNameInProcessShapeValidation_WhenAllowedActivatorSiteIsDocumented()
    {
        AllowedValidatorActivatorSites.ShouldContain(
            "src/Core/Validation/InProcessCreateCommandShapeValidation.cs");
        typeof(InProcessCreateCommandShapeValidation).Namespace
            .ShouldBe("ClearMeasure.Bootcamp.Core.Validation");
    }

    [Test]
    public void ShouldResideInUiShared_WhenValidationBehaviorIsThePipeline()
    {
        typeof(ValidationBehavior<,>).Namespace.ShouldBe("ClearMeasure.Bootcamp.UI.Shared");
        typeof(ValidationBehavior<,>).Assembly.ShouldBe(typeof(Bus).Assembly);
    }

    [Test]
    public void ShouldStayPure_WhenValidationBehaviorHasNoTypeExceptionsOrOptOutMarkers()
    {
        var source = RepositoryCheckout.Current.ReadText("src/UI.Shared/ValidationBehavior.cs");

        source.ShouldNotContain("IHttpEnvelopeValidation");
        source.ShouldNotContain("HttpOnlyFluentValidationRequests");
        source.ShouldNotContain("CreateVendorCommand");
        source.ShouldNotContain("CreateTeamCommand");
        source.ShouldNotContain("CreateDatedWorkOrdersCommand");
        source.ShouldNotContain("HttpEnvelopeFluentValidators");
    }

    [Test]
    public void ShouldResolveDeferredValidatorsFromDi_WhenHttpEnvelopeHandOffHasNoNewValidators()
    {
        var resolver = RepositoryCheckout.Current.ReadText(
            "src/UI/Server/Middleware/HttpEnvelopeFluentValidators.cs");
        var set = RepositoryCheckout.Current.ReadText(
            "src/UI/Server/Middleware/HttpEnvelopeValidatorSet.cs");

        resolver.ShouldContain("HttpEnvelopeValidatorSet");
        resolver.ShouldNotContain("new CreateVendorCommandValidator");
        set.ShouldContain("HttpEnvelopeValidationDeferral");
        set.ShouldContain("ActivatorUtilities.CreateInstance");
        set.ShouldNotContain("new CreateVendorCommandValidator");
        set.ShouldNotContain("new CreateTeamCommandValidator");
        set.ShouldNotContain("new CreateDatedWorkOrdersCommandValidator");
    }

    /// <summary>
    /// Explicit #1824 / HTTP-envelope exemptions: the only production files allowed to construct
    /// deferred FluentValidation types via <c>Activator</c> / <c>ActivatorUtilities</c>.
    /// </summary>
    internal static IReadOnlyList<string> AllowedValidatorActivatorSites { get; } =
    [
        "src/Core/Validation/InProcessCreateCommandShapeValidation.cs",
        "src/UI/Server/Middleware/HttpEnvelopeValidatorSet.cs"
    ];

    private static IReadOnlyList<string> EmptyAbstractValidators(RepositoryCheckout checkout)
    {
        var empty = new List<string>();
        foreach (var file in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(file);
            if (IsTestSource(relative))
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

    // ReSharper disable once MemberCanBePrivate.Global -- exercised by ValidationPipelineArchitectureRulesTests
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

    private static IReadOnlyList<string> ProductionValidatorNews(RepositoryCheckout checkout)
    {
        var abstractValidators = AbstractValidatorTypeNames(checkout);
        var offenders = new List<string>();
        foreach (var file in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(file);
            if (!IsProductionSource(relative)
                || relative.EndsWith("/ValidationBehavior.cs", StringComparison.Ordinal))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
            foreach (var creation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
            {
                if (!abstractValidators.Contains(SimpleTypeName(creation.Type)))
                {
                    continue;
                }

                offenders.Add($"{relative}:{creation.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            }
        }

        return offenders;
    }

    private static IReadOnlyList<string> ProductionValidatorActivatorSites(RepositoryCheckout checkout)
    {
        var sites = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in checkout.SourceFiles(".cs"))
        {
            var relative = checkout.Relative(file);
            if (!IsProductionSource(relative))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
            if (!root.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(IsValidatorActivatorCall))
            {
                continue;
            }

            sites.Add(relative);
        }

        return sites.ToList();
    }

    private static bool IsValidatorActivatorCall(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            IdentifierNameSyntax id => id.Identifier.ValueText,
            _ => null
        };

        return name is "CreateInstance";
    }

    private static HashSet<string> AbstractValidatorTypeNames(RepositoryCheckout checkout)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in checkout.SourceFiles(".cs"))
        {
            if (IsTestSource(checkout.Relative(file)))
            {
                continue;
            }

            var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetCompilationUnitRoot();
            foreach (var type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (DerivesFromAbstractValidator(type))
                {
                    names.Add(type.Identifier.ValueText);
                }
            }
        }

        return names;
    }

    private static string SimpleTypeName(TypeSyntax type) =>
        type is IdentifierNameSyntax id ? id.Identifier.ValueText
        : type is QualifiedNameSyntax q ? q.Right.Identifier.ValueText
        : type.ToString();

    private static bool IsTestSource(string relative) =>
        relative.StartsWith("src/UnitTests/", StringComparison.Ordinal)
        || relative.StartsWith("src/IntegrationTests/", StringComparison.Ordinal)
        || relative.StartsWith("src/AcceptanceTests/", StringComparison.Ordinal);

    private static bool IsProductionSource(string relative) =>
        relative.StartsWith("src/", StringComparison.Ordinal) && !IsTestSource(relative);

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
