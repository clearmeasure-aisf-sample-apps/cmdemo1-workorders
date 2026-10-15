using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.DemoData;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DemoData;

[TestFixture]
public class DemoEmployeeSeederTests
{
    // Each test seeds its own in-memory SQLite database, so its exact counts never depend on,
    // or change, the rows of the shared test database.
    private SqliteConnection _hold = null!;
    private string _cs = null!;

    [SetUp]
    public void SetUp()
    {
        _cs = $"Data Source=demo-seeder-{TestData.Tag()};Mode=Memory;Cache=Shared";
        _hold = new SqliteConnection(_cs);
        _hold.Open();
        using var ctx = NewContext();
        ctx.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _hold.Dispose();
    }

    [Test]
    public void ShouldAddEveryDemoRoleEmployeeAndLink_WhenDatabaseIsEmpty()
    {
        var result = Seed();

        result.RolesAdded.ShouldBe(9);
        result.RolesFound.ShouldBe(0);
        result.EmployeesAdded.ShouldBe(27);
        result.EmployeesFound.ShouldBe(0);
        result.RoleLinksAdded.ShouldBe(39);
        using var context = NewContext();
        var lovejoy = context.Set<Employee>().Include(e => e.Roles).Single(e => e.UserName == "tlovejoy");
        lovejoy.GetFullName().ShouldBe("Timothy Lovejoy Jr");
        lovejoy.EmailAddress.ShouldBe("reverend@firstchurchspringfield.org");
        lovejoy.PreferredLanguage.ShouldBe("en-US");
        lovejoy.Roles.Single().Name.ShouldBe("Minister");
        lovejoy.CanCreateWorkOrder().ShouldBeTrue();
        lovejoy.CanFulfillWorkOrder().ShouldBeTrue();
        context.Set<Employee>().Single(e => e.UserName == "jcuevas").PreferredLanguage.ShouldBe("es-ES");
    }

    [Test]
    public void ShouldAddNothing_WhenRunAgain()
    {
        Seed();

        var result = Seed();

        result.ShouldBe(new DemoEmployeeSeedResult(0, 9, 0, 27, 0));
        using var context = NewContext();
        context.Set<Employee>().Count().ShouldBe(27);
        context.Set<Role>().Count().ShouldBe(9);
    }

    [Test]
    public void ShouldKeepExistingEmployeeAndAddOnlyMissingRoles_WhenUserNameExists()
    {
        var deacon = new Role("Deacon", true, true);
        var existing = new Employee("nflanders", "Nedward", "Flanders", "ned@leftorium.com")
        {
            MiddleName = "Okily",
            PreferredLanguage = "fr-FR"
        };
        existing.AddRole(deacon);
        using (var context = NewContext())
        {
            context.Add(deacon);
            context.Add(existing);
            context.SaveChanges();
        }

        var result = Seed();

        result.RolesAdded.ShouldBe(8);
        result.RolesFound.ShouldBe(1);
        result.EmployeesAdded.ShouldBe(26);
        result.EmployeesFound.ShouldBe(1);
        result.RoleLinksAdded.ShouldBe(38);
        using var verify = NewContext();
        var ned = verify.Set<Employee>().Include(e => e.Roles).Single(e => e.UserName == "nflanders");
        ned.Id.ShouldBe(existing.Id);
        ned.FirstName.ShouldBe("Nedward");
        ned.MiddleName.ShouldBe("Okily");
        ned.EmailAddress.ShouldBe("ned@leftorium.com");
        ned.PreferredLanguage.ShouldBe("fr-FR");
        string.Join(", ", ned.Roles.Select(r => r.Name).Order()).ShouldBe("Deacon, Parishioner");
        var keptDeacon = verify.Set<Role>().Single(r => r.Name == "Deacon");
        keptDeacon.Id.ShouldBe(deacon.Id);
        keptDeacon.CanCreateWorkOrder.ShouldBeTrue();
    }

    private DemoEmployeeSeedResult Seed()
    {
        using var context = NewContext();
        return DemoEmployeeSeeder.Seed(context);
    }

    private DataContext NewContext()
    {
        return new DataContext(new StubDatabaseConfiguration(_cs));
    }

    private sealed class StubDatabaseConfiguration(string cs) : IDatabaseConfiguration
    {
        public string GetConnectionString()
        {
            return cs;
        }

        public void ResetConnectionPool()
        {
        }
    }
}
