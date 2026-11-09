using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class SavedSearchViewTests
{
    private static readonly string[] NoViews = [];

    [Test]
    public void ValidateNew_ValidNameAndQuery_ReturnsNull()
    {
        SavedSearchView.ValidateNew("My view", "Priority=High", NoViews).ShouldBeNull();
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public void ValidateNew_EmptyOrWhitespaceName_IsRejected(string? name)
    {
        SavedSearchView.ValidateNew(name, "Priority=High", NoViews).ShouldBe("Enter a name for the view.");
    }

    [Test]
    public void ValidateNew_NameOf101Characters_IsRejected()
    {
        SavedSearchView.ValidateNew(new string('a', 101), "", NoViews).ShouldBe("Name must be 100 characters or fewer.");
    }

    [Test]
    public void ValidateNew_NameOf100Characters_IsAccepted()
    {
        SavedSearchView.ValidateNew(new string('a', 100), "", NoViews).ShouldBeNull();
    }

    [Test]
    public void ValidateNew_QueryOver1000Characters_IsRejected()
    {
        SavedSearchView.ValidateNew("Long", "Q=" + new string('x', 999), NoViews)
            .ShouldBe("Current filters are too long to save.");
    }

    [Test]
    public void ValidateNew_DuplicateName_IsRejected()
    {
        SavedSearchView.ValidateNew("Mine", "", ["Mine"]).ShouldBe("A view named 'Mine' already exists.");
    }

    [Test]
    public void ValidateNew_DuplicateNameDifferentCase_IsRejected()
    {
        SavedSearchView.ValidateNew(" mine ", "", ["MINE"]).ShouldBe("A view named 'mine' already exists.");
    }

    [Test]
    public void ValidateNew_TwentyExistingViews_IsRejected()
    {
        var existing = Enumerable.Range(1, 20).Select(i => $"View {i}").ToArray();

        SavedSearchView.ValidateNew("One more", "", existing).ShouldBe("You can save up to 20 views. Delete one first.");
    }

    [Test]
    public void ValidateNew_NineteenExistingViews_IsAccepted()
    {
        var existing = Enumerable.Range(1, 19).Select(i => $"View {i}").ToArray();

        SavedSearchView.ValidateNew("One more", "", existing).ShouldBeNull();
    }
}
