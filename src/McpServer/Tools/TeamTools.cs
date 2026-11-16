using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

[McpServerToolType]
public class TeamTools
{
    [McpServerTool(Name = "list-teams"), Description("Lists all teams in the system with their name, description, and members.")]
    public static async Task<string> ListTeams(IBus bus)
    {
        var teams = await bus.Send(new TeamsQuery());
        return JsonSerializer.Serialize(teams.Select(TeamJson.From).ToArray(), McpJsonContext.Default.TeamJsonArray);
    }

    [McpServerTool(Name = "create-team"), Description("Creates a new team with optional description and members.")]
    public static async Task<string> CreateTeam(
        IBus bus,
        [Description("The name of the team")] string name,
        [Description("Optional description of what the team does")] string? description = null,
        [Description("Optional list of employee usernames to add as members")] string[]? memberUserNames = null)
    {
        try
        {
            var command = new CreateTeamCommand(name, description, memberUserNames ?? []);
            var result = await bus.Send(command);

            if (result.Succeeded)
            {
                return JsonSerializer.Serialize(
                    new CreatedJson(result.Succeeded, result.Message, result.Id),
                    McpCamelCaseJsonContext.Default.CreatedJson);
            }

            return result.Message;
        }
        catch (ValidationException ex)
        {
            return ex.Message;
        }
    }
}
