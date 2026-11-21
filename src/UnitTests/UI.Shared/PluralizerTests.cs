using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class PluralizerTests
{
    [Test]
    public void Count_WhenZero_ReturnsPluralForm()
    {
        Pluralizer.Count(0, "work order").ShouldBe("0 work orders");
    }

    [Test]
    public void Count_WhenOne_ReturnsSingularForm()
    {
        Pluralizer.Count(1, "work order").ShouldBe("1 work order");
    }

    [Test]
    public void Count_WhenTwo_ReturnsPluralForm()
    {
        Pluralizer.Count(2, "work order").ShouldBe("2 work orders");
    }

    [Test]
    public void Count_WhenCustomPluralGiven_ReturnsCustomPluralForm()
    {
        Pluralizer.Count(2, "child", "children").ShouldBe("2 children");
    }

    [Test]
    public void Count_WhenNegative_ReturnsInvariantMinusWithPluralForm()
    {
        Pluralizer.Count(-2, "work order").ShouldBe("-2 work orders");
    }
}
