using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

/// <summary>
/// Deletes only teams whose names carry a fixture-owned tag or prefix —
/// never a global wipe under <see cref="NUnit.Framework.ParallelScope.All"/>.
/// </summary>
public static class AcceptanceTeams
{
    /// <summary>
    /// Removes teams created with <paramref name="tag"/> in the name
    /// (see <c>CreateTaggedTeamAsync</c> / leftover <c>Team {prefix}…</c> names).
    /// </summary>
    public static void DeleteCreatedWithTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag is required for scoped team cleanup.", nameof(tag));
        }

        using var context = TestHost.NewDbContext();
        var teams = context.Set<Team>()
            .Include(team => team.Members)
            .Where(team => team.Name.Contains(tag))
            .ToList();
        if (teams.Count == 0)
        {
            return;
        }

        context.RemoveRange(teams);
        context.SaveChanges();
    }
}
