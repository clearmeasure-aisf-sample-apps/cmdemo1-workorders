using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

// The shapes the MCP tools answer with. Property order is the JSON order.

internal sealed record RoleJson(string Name, bool CanCreateWorkOrder, bool CanFulfillWorkOrder)
{
    public static RoleJson From(Role role) => new(role.Name, role.CanCreateWorkOrder, role.CanFulfillWorkOrder);
}

internal sealed record EmployeeJson
{
    public required string UserName { get; init; }
    public required string FirstName { get; init; }
    public string? MiddleName { get; init; }
    public required string LastName { get; init; }
    public required string EmailAddress { get; init; }
    public required RoleJson[] Roles { get; init; }

    public static EmployeeJson From(Employee employee) => new()
    {
        UserName = employee.UserName,
        FirstName = employee.FirstName,
        MiddleName = employee.MiddleName,
        LastName = employee.LastName,
        EmailAddress = employee.EmailAddress,
        Roles = employee.Roles.Select(RoleJson.From).ToArray()
    };
}

internal sealed record TeamMemberJson(string UserName, string FirstName, string LastName);

internal sealed record TeamJson(Guid Id, string Name, string? Description, TeamMemberJson[] Members)
{
    public static TeamJson From(Team team) => new(
        team.Id, team.Name, team.Description,
        team.Members.Select(m => new TeamMemberJson(m.UserName, m.FirstName, m.LastName)).ToArray());
}

internal sealed record VendorJson
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Trade { get; init; }
    public string? ContactName { get; init; }
    public string? EmailAddress { get; init; }
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }

    public static VendorJson From(Vendor vendor) => new()
    {
        Id = vendor.Id,
        Name = vendor.Name,
        Trade = vendor.Trade,
        ContactName = vendor.ContactName,
        EmailAddress = vendor.EmailAddress,
        PhoneNumber = vendor.PhoneNumber,
        IsActive = vendor.IsActive
    };
}

internal sealed record CreatedJson(bool Succeeded, string Message, Guid? Id);

internal sealed record WorkOrderSummaryJson
{
    public string? Number { get; init; }
    public string? Title { get; init; }
    public required string Status { get; init; }
    public string? Creator { get; init; }
    public string? Assignee { get; init; }
    public string? DueDate { get; init; }
    public required string Priority { get; init; }
    public string? Building { get; init; }
}

internal sealed record WorkOrderDetailJson
{
    public string? Number { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public required string Instructions { get; init; }
    public required string Status { get; init; }
    public string? RoomNumber { get; init; }
    public string? Creator { get; init; }
    public string? CreatorUsername { get; init; }
    public string? Assignee { get; init; }
    public string? AssigneeUsername { get; init; }
    public DateTime? CreatedDate { get; init; }
    public DateTime? AssignedDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public string? DueDate { get; init; }
    public required string Priority { get; init; }
    public string? Building { get; init; }
}

internal sealed record WorkOrderAttachmentJson
{
    public Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long FileSize { get; init; }
    public string? UploadedBy { get; init; }
    public string? UploadedByUsername { get; init; }
    public DateTime UploadedDate { get; init; }
}
