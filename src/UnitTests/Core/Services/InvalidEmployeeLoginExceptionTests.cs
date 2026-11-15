using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class InvalidEmployeeLoginExceptionTests
{
    [Test]
    public void Message_ShouldExplainMissingOrInvalidEmployee()
    {
        var ex = new InvalidEmployeeLoginException();

        ex.Message.ShouldContain("doesn't exist");
        ex.ShouldBeAssignableTo<Exception>();
    }
}
