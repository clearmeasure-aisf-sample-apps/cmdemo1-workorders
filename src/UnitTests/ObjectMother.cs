using System.Collections;
using System.Reflection;
using AutoBogus;
using AutoBogus.Conventions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests;

public static class ObjectMother
{
    private static readonly Lazy<bool> Configured = new(() =>
    {
        ConfigureBogus();
        return true;
    });

    private static void EnsureConfigured()
    {
        _ = Configured.Value;
    }

    public static TK Faker<TK>()
    {
        EnsureConfigured();
        return AutoFaker.Generate<TK>();
    }

    /// <summary>A draft work order, optionally created by <paramref name="createdBy"/>.</summary>
    public static WorkOrder DraftWorkOrder(Employee? createdBy = null) =>
        WorkOrderBuilder.Create()
            .CreatedBy(createdBy)
            .WithStatus(WorkOrderStatus.Draft)
            .Build();

    /// <summary>An employee with the given user name and optional display names.</summary>
    public static Employee Employee(
        string userName,
        string firstName = "Test",
        string lastName = "Employee",
        string? emailAddress = null) =>
        EmployeeBuilder.Create(userName)
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithEmail(emailAddress ?? $"{userName}@example.test")
            .Build();

    /// <summary>An employee who can fulfill work orders.</summary>
    public static Employee Fulfiller(string userName = "fulfiller") =>
        EmployeeBuilder.Create(userName)
            .WithFirstName("Groundskeeper")
            .WithLastName("Willie")
            .WithRole("Fulfiller", canCreate: false, canFulfill: true)
            .Build();

    /// <summary>An employee who can create work orders.</summary>
    public static Employee Creator(string userName = "creator") =>
        EmployeeBuilder.Create(userName)
            .WithFirstName("Ned")
            .WithLastName("Flanders")
            .WithRole("Creator", canCreate: true, canFulfill: false)
            .Build();


    private static void ConfigureBogus()
    {
        AutoFaker.Configure(builder =>
        {
            builder.WithConventions()
                .WithSkip<WorkOrder>(wo => wo.Creator)
                .WithSkip<WorkOrder>(wo => wo.Assignee)
                .WithSkip<WorkOrder>(wo => wo.DueDate)
                .WithSkip<WorkOrder>(wo => wo.WaitingOnVendorSince)
                .WithSkip<Employee>(wo => wo.Roles)
                .WithOverride(new BogusOverrides());
        });
    }

    public static void AssertAllProperties(object expected, object actual)
    {
        if (expected.GetType().IsArray)
        {
            actual.ShouldBeEquivalentTo(expected);
            return;
        }

        var properties = expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            AssertComparableProperty(property, expected, actual);
        }
    }

    private static void AssertComparableProperty(PropertyInfo property, object expected, object actual)
    {
        if (IsNonStringEnumerable(property.PropertyType))
        {
            return;
        }

        var expectedValue = property.GetValue(expected, null);
        var actualValue = property.GetValue(actual, null);
        if (Equals(expectedValue, actualValue))
        {
            return;
        }

        FailMismatchedProperty(property, expectedValue, actualValue);
    }

    private static bool IsNonStringEnumerable(Type propertyType)
    {
        return typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string);
    }

    private static void FailMismatchedProperty(PropertyInfo property, object? expectedValue, object? actualValue)
    {
        if (property.DeclaringType == null)
        {
            return;
        }

        Assert.Fail(
            $"Property {property.DeclaringType.Name}.{property.Name} does not match. Expected: {expectedValue} but was: {actualValue}");
    }
}