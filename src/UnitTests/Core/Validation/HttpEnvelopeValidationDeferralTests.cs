using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class HttpEnvelopeValidationDeferralTests
{
    [Test]
    public void ShouldListExactlyTheThreeDeferredValidators_WhenCatalogIsRead()
    {
        HttpEnvelopeValidationDeferral.ValidatorTypes.ShouldBe(
        [
            typeof(CreateVendorCommandValidator),
            typeof(CreateTeamCommandValidator),
            typeof(CreateDatedWorkOrdersCommandValidator)
        ]);
    }

    [Test]
    public void ShouldMapEachDeferredRequest_WhenValidatorTypeForRequestIsCalled()
    {
        HttpEnvelopeValidationDeferral.ValidatorTypeForRequest(typeof(CreateVendorCommand))
            .ShouldBe(typeof(CreateVendorCommandValidator));
        HttpEnvelopeValidationDeferral.ValidatorTypeForRequest(typeof(CreateTeamCommand))
            .ShouldBe(typeof(CreateTeamCommandValidator));
        HttpEnvelopeValidationDeferral.ValidatorTypeForRequest(typeof(CreateDatedWorkOrdersCommand))
            .ShouldBe(typeof(CreateDatedWorkOrdersCommandValidator));
    }

    [Test]
    public void ShouldReturnNull_WhenRequestIsNotDeferred()
    {
        HttpEnvelopeValidationDeferral.ValidatorTypeForRequest(typeof(SaveDraftCommand)).ShouldBeNull();
    }

    [Test]
    public void IsDeferredValidator_ShouldMatchTheCatalog()
    {
        HttpEnvelopeValidationDeferral.IsDeferredValidator(typeof(CreateVendorCommandValidator)).ShouldBeTrue();
        HttpEnvelopeValidationDeferral.IsDeferredValidator(typeof(SaveDraftCommandValidator)).ShouldBeFalse();
    }

    [Test]
    public void Entries_ShouldMatchValidatorTypes_WhenCatalogIsRead()
    {
        HttpEnvelopeValidationDeferral.Entries.Select(entry => entry.ValidatorType)
            .ShouldBe(HttpEnvelopeValidationDeferral.ValidatorTypes);
    }
}
