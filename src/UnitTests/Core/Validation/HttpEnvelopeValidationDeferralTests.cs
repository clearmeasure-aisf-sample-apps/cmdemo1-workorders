using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class HttpEnvelopeValidationDeferralTests
{
    [Test]
    public void Entries_ShouldListVendorTeamAndDated_WhenCatalogIsRead()
    {
        HttpEnvelopeValidationDeferral.Entries.Select(entry => entry.ValidatorType).ShouldBe(
        [
            typeof(CreateVendorCommandValidator),
            typeof(CreateTeamCommandValidator),
            typeof(CreateDatedWorkOrdersCommandValidator)
        ]);
        HttpEnvelopeValidationDeferral.Entries.Select(entry => entry.RequestType).ShouldBe(
        [
            typeof(CreateVendorCommand),
            typeof(CreateTeamCommand),
            typeof(CreateDatedWorkOrdersCommand)
        ]);
    }
}
