namespace ClearMeasure.Bootcamp.Core.Model;

public class Employee : EntityBase<Employee>, IComparable<Employee>
{
    public Employee()
    {
        UserName = null!;
        EmailAddress = null!;
        FirstName = null!;
        LastName = null!;
    }

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
    public Employee(string userName, string firstName, string lastName, string emailAddress)
    {
        UserName = userName;
        FirstName = firstName;
        LastName = lastName;
        EmailAddress = emailAddress;
    }

    public override Guid Id { get; set; }

    public string UserName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    /// <summary>
    /// Optional middle name. When null or blank it is omitted from <see cref="GetFullName"/>.
    /// </summary>
    public string? MiddleName { get; set; }

    public string EmailAddress { get; set; }

    public string PreferredLanguage { get; set; } = "en-US";

    public ISet<Role> Roles { get; init; } = new HashSet<Role>();

    public int CompareTo(Employee? other)
    {
        var compareResult = string.Compare(LastName, other!.LastName, StringComparison.Ordinal);
        if (compareResult == 0)
        {
            compareResult = string.Compare(FirstName, other.FirstName, StringComparison.Ordinal);
        }

        return compareResult;
    }

    public string GetFullName()
    {
        return string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";
    }

    /// <summary>
    /// Returns "First Last" when both names are set, the one name that is set, or the user name when both are empty.
    /// A null, empty, or whitespace-only name counts as empty. The middle name is omitted.
    /// </summary>
    public string DisplayName
    {
        get
        {
            var hasFirst = !string.IsNullOrWhiteSpace(FirstName);
            var hasLast = !string.IsNullOrWhiteSpace(LastName);
            if (hasFirst && hasLast)
            {
                return $"{FirstName.Trim()} {LastName.Trim()}";
            }

            if (hasFirst)
            {
                return FirstName.Trim();
            }

            return hasLast ? LastName.Trim() : UserName;
        }
    }

    /// <summary>
    /// Returns the employee's display name as "LastName, FirstName". If either name is missing, returns the other;
    /// if both names are missing, returns the username.
    /// </summary>
    public string GetDisplayName()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            return string.IsNullOrWhiteSpace(LastName) ? UserName : LastName;
        }

        return string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{LastName}, {FirstName}";
    }

    /// <summary>
    /// Returns the uppercase initials of this employee's first and last names.
    /// </summary>
    public string GetInitials()
    {
        return $"{GetInitial(FirstName)}{GetInitial(LastName)}";
    }

    public override string ToString()
    {
        return GetFullName();
    }

    public bool CanCreateWorkOrder() => Roles.Any(role => role.CanCreateWorkOrder);

    /// <summary>
    /// Returns whether this employee has at least one role with permission to fulfill work orders.
    /// </summary>
    public bool CanFulfillWorkOrder() => Roles.Any(role => role.CanFulfillWorkOrder);

    /// <summary>
    /// Returns true when any role's name equals <paramref name="roleName"/>, ignoring case.
    /// Returns false when <paramref name="roleName"/> is null or empty.
    /// </summary>
    public bool HasRole(string? roleName)
    {
        if (string.IsNullOrEmpty(roleName))
        {
            return false;
        }

        return Roles.Any(role => string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase));
    }

    public void AddRole(Role role)
    {
        Roles.Add(role);
    }

    private static string GetInitial(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? string.Empty : char.ToUpperInvariant(name.Trim()[0]).ToString();
    }
}