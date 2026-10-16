namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// A named group of employees, such as the grounds crew or the kitchen team.
/// </summary>
public class Team : EntityBase<Team>
{
    /// <summary>
    /// Maximum length of <see cref="Name"/>.
    /// </summary>
    public const int NameMaxLength = 100;

    /// <summary>
    /// Maximum length of <see cref="Description"/>.
    /// </summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>
    /// Creates an empty team. Used by EF Core and serialization.
    /// </summary>
    public Team()
    {
        Name = null!;
    }

    /// <summary>
    /// Creates a team with a name and an optional description.
    /// </summary>
    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public Team(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Unique identifier of the team.
    /// </summary>
    public override Guid Id { get; set; }

    /// <summary>
    /// Name of the team, unique ignoring case.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Optional description of what the team does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Employees who belong to the team. An employee appears at most once.
    /// </summary>
    public ISet<Employee> Members { get; init; } = new HashSet<Employee>();

    /// <summary>
    /// Number of employees in <see cref="Members"/>.
    /// </summary>
    public int MemberCount => Members.Count;

    /// <summary>
    /// Adds an employee to the team. Adding an employee who is already a member does nothing.
    /// </summary>
    public void AddMember(Employee employee) => Members.Add(employee);
}
