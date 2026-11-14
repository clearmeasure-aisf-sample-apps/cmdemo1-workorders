using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.AcceptanceTests.TestUsers;

/// <summary>
/// Clears persisted teams so parallel page scans do not see leftover team members
/// (member avatars fail axe <c>label-content-name-mismatch</c> with master markup).
/// </summary>
public static class AcceptanceTeams
{
    /// <summary>Deletes every team (join rows cascade).</summary>
    public static void ClearAll()
    {
        using var context = TestHost.NewDbContext();
        context.Set<Team>().ExecuteDelete();
    }
}
