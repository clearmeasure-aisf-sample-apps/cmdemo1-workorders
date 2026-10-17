using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class BuildingRoomSummaryQueryValidatorTests
{
    [Test]
    public void ShouldPass_ForBuildingRoomSummaryQuery()
    {
        new BuildingRoomSummaryQueryValidator().TestValidate(new BuildingRoomSummaryQuery()).IsValid.ShouldBeTrue();
    }
}
