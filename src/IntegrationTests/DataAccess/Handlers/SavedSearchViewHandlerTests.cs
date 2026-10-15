using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class SavedSearchViewHandlerTests
{
    private Employee _owner = null!;
    private Employee _other = null!;

    [SetUp]
    public async Task SetUp()
    {
        _owner = NewEmployee("svowner", "Owner");
        _other = NewEmployee("svother", "Other");
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(_owner, _other);
        await context.SaveChangesAsync();
    }

    private static Employee NewEmployee(string prefix, string lastName)
    {
        var userName = TestData.UserName(prefix);
        return new Employee(userName, "Saved", lastName, TestData.Email(userName));
    }

    private static SavedSearchViewHandler NewHandler() =>
        new(TestHost.GetRequiredService<DataContext>(), TimeProvider.System);

    [Test]
    public async Task Save_ThenList_ReturnsViewWithQuery()
    {
        var result = await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, " Urgent ", "Priority=High&OverdueOnly=true"));

        result.Succeeded.ShouldBeTrue();
        var views = await NewHandler().Handle(new SavedSearchViewsQuery(_owner.UserName));
        views.Length.ShouldBe(1);
        views[0].Name.ShouldBe("Urgent");
        views[0].Query.ShouldBe("Priority=High&OverdueOnly=true");
        views[0].EmployeeId.ShouldBe(_owner.Id);
    }

    [Test]
    public async Task List_ReturnsOnlyOwnViews_OrderedByName()
    {
        await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, "zeta", "Q=z"));
        await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, "Alpha", "Q=a"));
        await NewHandler().Handle(new SaveSearchViewCommand(_other.UserName, "Beta", "Q=b"));

        var views = await NewHandler().Handle(new SavedSearchViewsQuery(_owner.UserName));

        views.Select(v => v.Name).ShouldBe(["Alpha", "zeta"]);
        (await NewHandler().Handle(new SavedSearchViewsQuery("nobody"))).ShouldBeEmpty();
    }

    [Test]
    public async Task Delete_RemovesOwnView()
    {
        await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, "Mine", "Q=m"));
        var view = (await NewHandler().Handle(new SavedSearchViewsQuery(_owner.UserName))).Single();

        var result = await NewHandler().Handle(new DeleteSearchViewCommand(_owner.UserName, view.Id));

        result.Succeeded.ShouldBeTrue();
        (await NewHandler().Handle(new SavedSearchViewsQuery(_owner.UserName))).ShouldBeEmpty();
    }

    [Test]
    public async Task Delete_AnotherUsersView_FailsAndRowRemains()
    {
        await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, "Mine", "Q=m"));
        var view = (await NewHandler().Handle(new SavedSearchViewsQuery(_owner.UserName))).Single();

        var result = await NewHandler().Handle(new DeleteSearchViewCommand(_other.UserName, view.Id));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("View not found.");
        (await NewHandler().Handle(new SavedSearchViewsQuery(_owner.UserName))).Length.ShouldBe(1);
    }

    [Test]
    public async Task Save_DuplicateName_Fails()
    {
        await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, "Mine", "Q=m"));

        var result = await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, "MINE", "Q=n"));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("A view named 'MINE' already exists.");
    }

    [Test]
    public async Task Save_TwentyFirstView_Fails()
    {
        for (var i = 1; i <= SavedSearchView.MaxViewsPerEmployee; i++)
        {
            (await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, $"View {i}", $"Q={i}"))).Succeeded.ShouldBeTrue();
        }

        var result = await NewHandler().Handle(new SaveSearchViewCommand(_owner.UserName, "View 21", "Q=21"));

        result.Succeeded.ShouldBeFalse();
        result.Error.ShouldBe("You can save up to 20 views. Delete one first.");
        (await NewHandler().Handle(new SavedSearchViewsQuery(_owner.UserName))).Length.ShouldBe(20);
    }
}
