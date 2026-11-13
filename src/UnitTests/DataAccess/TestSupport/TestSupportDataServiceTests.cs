using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.TestSupport;

[TestFixture]
public class TestSupportDataServiceTests
{
    [Test]
    public void ShouldExposeCreateMethods_WhenConstructedWithNullsDeferred()
    {
        typeof(TestSupportDataService).GetMethod(nameof(TestSupportDataService.CreateEmployeeAsync)).ShouldNotBeNull();
        typeof(TestSupportDataService).GetMethod(nameof(TestSupportDataService.CreateWorkOrderAsync)).ShouldNotBeNull();
    }
}
