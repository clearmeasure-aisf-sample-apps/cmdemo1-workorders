using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
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
        return JsonSerializer.Serialize(teams.Select(FormatTeam).ToArray(),
            new JsonSerializerOptions { WriteIndented = true });
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
                var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Serialize(new
                {
                    result.Succeeded,
                    result.Message,
                    result.Id
                }, options);
            }

            return result.Message;
        }
        catch (ValidationException ex)
        {
            return ex.Message;
        }
    }

    private static object FormatTeam(Team team) => new
    {
        team.Id,
        team.Name,
        team.Description,
        Members = team.Members.Select(m => new
        {
            m.UserName,
            m.FirstName,
            m.LastName
        }).ToArray()
    };
}
