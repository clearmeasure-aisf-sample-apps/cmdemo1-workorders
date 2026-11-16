using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;
public class ReformatWorkOrderCommandTests
{
    [Test]
    public void Constructor_StoresNumberTitleAndDescription()
    {
        var command = new ReformatWorkOrderCommand("WO-1", "Title", "Description");

        command.Number.ShouldBe("WO-1");
        command.Title.ShouldBe("Title");
        command.Description.ShouldBe("Description");
    }
}

