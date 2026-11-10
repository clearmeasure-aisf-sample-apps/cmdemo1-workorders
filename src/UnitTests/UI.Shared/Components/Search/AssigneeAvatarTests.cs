using Bunit;
using ClearMeasure.Bootcamp.UI.Shared.Components.Search;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components.Search;

[TestFixture]
public class AssigneeAvatarTests
{
    [Test]
    public async Task ShouldShowTheAssigneesInitials_WhenTheRowIsAssigned()
    {
        await using var ctx = new BunitContext();
        var row = SearchResultRowMother.AssignedToAda();

        var avatar = ctx.Render<AssigneeAvatar>(parameters => parameters.Add(p => p.Row, row));

        avatar.Find(".avatar").TextContent.ShouldBe("AL");
        avatar.Find(".avatar").ClassList.ShouldContain(row.AvatarCssClass);
    }

    [Test]
    public async Task ShouldShowAnEmptyAvatar_WhenTheRowIsUnassigned()
    {
        await using var ctx = new BunitContext();

        var avatar = ctx.Render<AssigneeAvatar>(parameters => parameters.Add(p => p.Row, SearchResultRowMother.Unassigned()));

        avatar.Find(".avatar").ClassList.ShouldContain("avatar--empty");
        avatar.Find(".avatar").TextContent.ShouldBeEmpty();
    }
}
