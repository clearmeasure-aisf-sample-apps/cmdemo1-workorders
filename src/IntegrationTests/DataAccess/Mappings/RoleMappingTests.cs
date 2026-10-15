using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class RoleMappingTests
{
    [Test]
    public void ShouldPersistRoles()
    {
        var name = $"role_{TestData.Tag()}";
        var role = new Role(name, true, true);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(role);
            context.SaveChanges();
        }

        Role rehydratedRole;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedRole = context.Set<Role>()
                .Single(r => r.Id == role.Id);
        }

        rehydratedRole.Id.ShouldBe(role.Id);
        rehydratedRole.Name.ShouldBe(name);
        rehydratedRole.CanCreateWorkOrder.ShouldBeTrue();
        rehydratedRole.CanFulfillWorkOrder.ShouldBeTrue();
    }
}