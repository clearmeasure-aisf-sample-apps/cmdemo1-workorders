using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.TestSupport;

[TestFixture]
public class UniqueWorkOrderNumberTests
{
    private const string Base36Pattern = "^[0-9A-Z]+$";

    [Test]
    public void Next_WithoutPrefix_ReturnsSevenUppercaseBase36Characters()
    {
        var number = UniqueWorkOrderNumber.Next();

        number.Length.ShouldBe(UniqueWorkOrderNumber.Length);
        number.ShouldMatch(Base36Pattern);
    }

    [Test]
    public void Next_WithPrefix_ReturnsSevenCharactersStartingWithPrefix()
    {
        var number = UniqueWorkOrderNumber.Next("W");

        number.Length.ShouldBe(UniqueWorkOrderNumber.Length);
        number.ShouldStartWith("W");
        number.ShouldMatch(Base36Pattern);
    }

    [Test]
    public void Next_CalledRepeatedly_ReturnsDistinctNumbers()
    {
        var numbers = Enumerable.Range(0, 200).Select(_ => UniqueWorkOrderNumber.Next()).ToArray();

        numbers.Distinct().Count().ShouldBe(numbers.Length);
    }

    [Test]
    public void Next_WithPrefixLongerThanOneCharacter_Throws()
    {
        Should.Throw<ArgumentException>(() => UniqueWorkOrderNumber.Next("WO"))
            .ParamName.ShouldBe("prefix");
    }

    [Test]
    public void Series_WithCount_ReturnsOrdinallySortedNumbersSharingAStem()
    {
        var numbers = UniqueWorkOrderNumber.Series(12);

        numbers.Length.ShouldBe(12);
        numbers.ShouldAllBe(n => n.Length == UniqueWorkOrderNumber.Length);
        numbers.Select(n => n[..5]).Distinct().Count().ShouldBe(1);
        numbers[0].ShouldEndWith("01");
        numbers[11].ShouldEndWith("12");
        numbers.Order(StringComparer.Ordinal).ShouldBe(numbers);
    }

    [TestCase(0)]
    [TestCase(100)]
    public void Series_WithCountOutOfRange_Throws(int count)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => UniqueWorkOrderNumber.Series(count));
    }

    [Test]
    public void WorkOrderBuilder_For_AssignsNumberFromSharedSource()
    {
        var creator = EmployeeBuilder.Create("creator").Build();

        var order = WorkOrderBuilder.For(creator, WorkOrderStatus.Draft).Build();

        order.Number!.Length.ShouldBe(UniqueWorkOrderNumber.Length);
        order.Number.ShouldMatch(Base36Pattern);
    }
}
