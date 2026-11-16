using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.AotHandlers;

/// <summary>The precompiled reads the Native AOT handlers share.</summary>
[TestFixture]
public class WorkOrderSnapshotSourceTests
{
    private AotParityDatabase _db = null!;

    [SetUp]
    public async Task SetUp() => _db = await AotParityDatabase.SeedAsync();

    [TearDown]
    public async Task TearDown() => await _db.DisposeAsync();

    [Test]
    public async Task LoadAsync_Should_ReadEveryWorkOrder()
    {
        var snapshot = await new WorkOrderSnapshotSource(_db.Context, _db.Clock).LoadAsync(CancellationToken.None);

        snapshot.All.Count.ShouldBe(7);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task FindEmployeeAsync_Should_ReturnNull_When_TheNameIsBlank(string? userName)
    {
        var source = new WorkOrderSnapshotSource(_db.Context, _db.Clock);

        (await source.FindEmployeeAsync(userName, CancellationToken.None)).ShouldBeNull();
    }

    [Test]
    public async Task FindEmployeeAsync_Should_FindTheEmployeeByUserName()
    {
        var source = new WorkOrderSnapshotSource(_db.Context, _db.Clock);

        (await source.FindEmployeeAsync("ffixer", CancellationToken.None))!.FirstName.ShouldBe("Fran");
    }

    [Test]
    public async Task EmployeesWithRolesAsync_Should_IncludeEachRole()
    {
        var employees = await new WorkOrderSnapshotSource(_db.Context, _db.Clock).EmployeesWithRolesAsync(CancellationToken.None);

        employees.Single(employee => employee.UserName == "pcreator").Roles.ShouldHaveSingleItem().Name.ShouldBe("Creator");
    }
}
