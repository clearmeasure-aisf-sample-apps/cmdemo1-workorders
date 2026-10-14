using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class NavQueueMatcherTests
{
    private const string BaseUri = "https://localhost/";

    [TestCase("https://localhost/workorder/search?Assignee=tlovejoy", "workorder/search?Assignee=tlovejoy", true)]
    [TestCase("https://localhost/workorder/search?Status=InProgress&Creator=a", "workorder/search?Creator=a&Status=InProgress", true)]
    [TestCase("https://localhost/workorder/search/", "workorder/search", true)]
    [TestCase("https://localhost/workorder/search?Assignee=tlovejoy&Priority=High", "workorder/search?Assignee=tlovejoy", false)]
    [TestCase("https://localhost/workorder/search", "workorder/search?OverdueOnly=true", false)]
    [TestCase("https://localhost/settings?OverdueOnly=true", "workorder/search?OverdueOnly=true", false)]
    [TestCase("https://localhost/workorder/search?Assignee=t%20l", "workorder/search?Assignee=t l", true)]
    public void IsActive_ComparesPathAndQuery(string current, string href, bool expected)
    {
        NavQueueMatcher.IsActive(current, BaseUri, href).ShouldBe(expected);
    }
}
