using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

/// <summary>
/// Persist and handle steps for state-command integration tests. Creates no entities.
/// </summary>
public static class StateCommandHandlerSteps
{
    /// <summary>Adds and saves the given entities.</summary>
    public static async Task PersistAsync(params object[] entities)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    /// <summary>Remotes <paramref name="command"/> and runs it through <see cref="StateCommandHandler"/>.</summary>
    public static async Task<StateCommandResult> HandleRemotedAsync(StateCommandBase command)
    {
        var remoted = RemotableRequestTests.SimulateRemoteObject(command);
        return await TestHost.GetRequiredService<StateCommandHandler>().Handle(remoted);
    }

    /// <summary>Loads a work order by id from a fresh context.</summary>
    public static WorkOrder RequireOrder(Guid id) =>
        TestHost.GetRequiredService<DbContext>().Find<WorkOrder>(id)
        ?? throw new InvalidOperationException();

    /// <summary>Loads a work order by number with no tracking.</summary>
    public static async Task<WorkOrder> LoadByNumberAsync(string number)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        return await context.Set<WorkOrder>().AsNoTracking().SingleAsync(w => w.Number == number);
    }

    /// <summary>Loads a work order by id with no tracking.</summary>
    public static async Task<WorkOrder> LoadByIdAsync(Guid id)
    {
        await using var context = TestHost.GetRequiredService<DbContext>();
        return await context.Set<WorkOrder>().AsNoTracking().SingleAsync(w => w.Id == id);
    }
}
