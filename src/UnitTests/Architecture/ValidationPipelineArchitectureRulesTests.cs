using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Architecture;

[TestFixture]
public class ValidationPipelineArchitectureRulesTests
{
    [Test]
    public void EmptyAbstractValidatorNames_WhenSampleIsSemicolonBody_ReportsTheType()
    {
        ValidationPipelineArchitectureTests.EmptyAbstractValidatorNames("""
            using FluentValidation;

            namespace Sample;

            public sealed class EmptyQueryValidator : AbstractValidator<string>;
            """).ShouldBe(["EmptyQueryValidator"]);
    }

    [Test]
    public void EmptyAbstractValidatorNames_WhenSampleHasRules_IsEmpty()
    {
        ValidationPipelineArchitectureTests.EmptyAbstractValidatorNames("""
            using FluentValidation;

            namespace Sample;

            public sealed class NamedQueryValidator : AbstractValidator<string>
            {
                public NamedQueryValidator()
                {
                    RuleFor(x => x).NotEmpty();
                }
            }
            """).ShouldBeEmpty();
    }
}
