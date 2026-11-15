using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class HttpEnvelopeValidationDeferralTests
{
    [Test]
    public void ShouldExposeVendorTeamAndDated_WhenCatalogIsRead()
    {
        HttpEnvelopeValidationDeferral.ValidatorTypes.ShouldBe(
        [
            typeof(CreateVendorCommandValidator),
            typeof(CreateTeamCommandValidator),
            typeof(CreateDatedWorkOrdersCommandValidator)
        ]);
        HttpEnvelopeValidationDeferral.Entries.Select(entry => entry.ValidatorType)
            .ShouldBe(HttpEnvelopeValidationDeferral.ValidatorTypes);
        HttpEnvelopeValidationDeferral.ValidatorTypeForRequest(typeof(CreateVendorCommand))
            .ShouldBe(typeof(CreateVendorCommandValidator));
        HttpEnvelopeValidationDeferral.ValidatorTypeForRequest(typeof(SaveDraftCommand)).ShouldBeNull();
        HttpEnvelopeValidationDeferral.IsDeferredValidator(typeof(CreateTeamCommandValidator)).ShouldBeTrue();
        HttpEnvelopeValidationDeferral.IsDeferredValidator(typeof(SaveDraftCommandValidator)).ShouldBeFalse();
    }
}
