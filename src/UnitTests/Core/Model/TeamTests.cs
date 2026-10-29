using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class TeamTests
{
    [Test]
    public void ShouldInitializeWithEmptyMembersAndZeroCount()
    {
        var team = new Team();

        team.Id.ShouldBe(Guid.Empty);
        team.Name.ShouldBeNull();
        team.Description.ShouldBeNull();
        team.Members.ShouldBeEmpty();
        team.MemberCount.ShouldBe(0);
    }

    [Test]
    public void ShouldSetNameAndDescriptionFromConstructor()
    {
        var team = new Team("Grounds", "Lawn and snow");

        team.Name.ShouldBe("Grounds");
        team.Description.ShouldBe("Lawn and snow");
        team.MemberCount.ShouldBe(0);
    }

    [Test]
    public void ShouldAddMemberAndIncreaseMemberCount()
    {
        var team = new Team("Grounds", null);
        var first = new Employee("u1", "Una", "One", "u1@example.com") { Id = Guid.NewGuid() };
        var second = new Employee("u2", "Dos", "Two", "u2@example.com") { Id = Guid.NewGuid() };

        team.AddMember(first);
        team.AddMember(second);

        team.MemberCount.ShouldBe(2);
        team.Members.ShouldContain(first);
        team.Members.ShouldContain(second);
    }

    [Test]
    public void ShouldNotAddSameEmployeeTwice()
    {
        var team = new Team("Grounds", null);
        var id = Guid.NewGuid();
        var employee = new Employee("u1", "Una", "One", "u1@example.com") { Id = id };
        var sameIdentity = new Employee("u1", "Una", "One", "u1@example.com") { Id = id };

        team.AddMember(employee);
        team.AddMember(employee);
        team.AddMember(sameIdentity);

        team.MemberCount.ShouldBe(1);
    }

    [Test]
    public void ShouldExposeNameAndDescriptionMaxLengths()
    {
        Team.NameMaxLength.ShouldBe(100);
        Team.DescriptionMaxLength.ShouldBe(500);
    }
}
