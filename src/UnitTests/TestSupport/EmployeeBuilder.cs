using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.UnitTests.TestSupport;

/// <summary>
/// Fluent test-data builder for <see cref="Employee"/>. The only place test projects should write
/// <c>new Employee(</c> when introducing named employees.
/// </summary>
public sealed class EmployeeBuilder
{
    private string _userName = "employee";
    private string _firstName = "Test";
    private string _lastName = "Employee";
    private string _emailAddress = "employee@example.test";
    private string? _middleName;
    private Guid? _id;
    private readonly List<Role> _roles = [];

    /// <summary>Starts a builder with default names derived from a user name.</summary>
    public static EmployeeBuilder Create(string userName = "employee") =>
        new EmployeeBuilder().WithUserName(userName);

    /// <summary>Sets the unique user name and a matching example.test e-mail when the e-mail was still default.</summary>
    // ReSharper disable once MemberCanBePrivate.Global -- fluent builder API used from Create and call sites
    public EmployeeBuilder WithUserName(string userName)
    {
        _userName = userName;
        if (_emailAddress == "employee@example.test")
        {
            _emailAddress = $"{userName}@example.test";
        }

        return this;
    }

    /// <summary>Sets the given name.</summary>
    public EmployeeBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    /// <summary>Sets the optional middle name.</summary>
    public EmployeeBuilder WithMiddleName(string? middleName)
    {
        _middleName = middleName;
        return this;
    }

    /// <summary>Sets the family name.</summary>
    public EmployeeBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    /// <summary>Sets the e-mail address.</summary>
    public EmployeeBuilder WithEmail(string emailAddress)
    {
        _emailAddress = emailAddress;
        return this;
    }

    /// <summary>Sets a fixed identity for remoting or equality assertions.</summary>
    public EmployeeBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    /// <summary>Adds a role with the given create/fulfill flags.</summary>
    public EmployeeBuilder WithRole(string name, bool canCreate, bool canFulfill)
    {
        _roles.Add(new Role(name, canCreate, canFulfill));
        return this;
    }

    /// <summary>Builds the employee.</summary>
    public Employee Build()
    {
        var employee = new Employee(_userName, _firstName, _lastName, _emailAddress)
        {
            MiddleName = _middleName
        };
        if (_id.HasValue)
        {
            employee.Id = _id.Value;
        }

        foreach (var role in _roles)
        {
            employee.AddRole(role);
        }

        return employee;
    }
}
