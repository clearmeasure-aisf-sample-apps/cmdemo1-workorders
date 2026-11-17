using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.TestSupport;

[TestFixture]
public class TestSupportEmployeeDtoTests
{
    [Test]
    public void ShouldRoundTrip_WhenInitialized()
    {
        var dto = new TestSupportEmployeeDto
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UserName = "u",
            FirstName = "f",
            LastName = "l",
            EmailAddress = "e@t"
        };
        dto.UserName.ShouldBe("u");
        dto.EmailAddress.ShouldBe("e@t");
    }
}
