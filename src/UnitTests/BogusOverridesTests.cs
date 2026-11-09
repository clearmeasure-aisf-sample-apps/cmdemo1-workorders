using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests;

[TestFixture]
public class BogusOverridesTests
{
    [Test]
    public void ShouldClampGeneratedRoomNumberToTheFormEntryLimit()
    {
        for (var i = 0; i < 30; i++)
        {
            var order = ObjectMother.Faker<WorkOrder>();
            order.RoomNumber.ShouldNotBeNullOrEmpty();
            order.RoomNumber!.Length.ShouldBeLessThanOrEqualTo(WorkOrder.RoomNumberEntryMaxLength);
        }
    }
}
