using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Mappings;

[TestFixture]
public class RoleMappingTests
{
    [Test, Retry(2)]
    public void ShouldPersistRoles()
    {
        // Create and save a Role with a unique name
        var name = $"role_{TestData.Tag()}";
        var role = new Role(name, true, true);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(role);
            context.SaveChanges();
        }

        // Retrieve the saved Role
        Role rehydratedRole;
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            rehydratedRole = context.Set<Role>()
                .Single(r => r.Id == role.Id);
        }

        // Assert
        rehydratedRole.Id.ShouldBe(role.Id);
        rehydratedRole.Name.ShouldBe(name);
        rehydratedRole.CanCreateWorkOrder.ShouldBeTrue();
        rehydratedRole.CanFulfillWorkOrder.ShouldBeTrue();
    }
}