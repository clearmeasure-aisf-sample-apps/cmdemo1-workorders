using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class CountSectionTests
{
    [Test]
    public void ShouldListTheCountsInSectionOrder_WhenSomeKeysHaveNoCount()
    {
        var section = new CountSection("Status", "Open by Status", ["Draft", "Assigned", "InProgress"]);

        var listed = section.Listed(new Dictionary<string, int> { ["InProgress"] = 2, ["Draft"] = 5 });

        listed.ShouldBe([KeyValuePair.Create("Draft", 5), KeyValuePair.Create("InProgress", 2)]);
    }
}
