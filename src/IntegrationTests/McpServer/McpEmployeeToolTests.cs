using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpEmployeeToolTests
{
    [Test]
    public async Task ShouldListAllEmployees()
    {
        var emp1 = new Employee(TestData.UserName("jdoe"), "John", "Doe", "john@test.com");
        var emp2 = new Employee(TestData.UserName("jsmith"), "Jane", "Smith", "jane@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp1);
        context.Add(emp2);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.ListEmployees(bus);

        result.ShouldContain(emp1.UserName);
        result.ShouldContain(emp2.UserName);
        result.ShouldContain("John");
        result.ShouldContain("Jane");
    }

    [Test]
    public async Task ShouldReturnJsonArrayWithoutUnknownEmployee()
    {
        var unknown = TestData.UserName("nobody");
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.ListEmployees(bus);

        using var json = JsonDocument.Parse(result);
        json.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        result.ShouldNotContain(unknown);
    }

    [Test]
    public async Task ShouldGetEmployeeByUsername()
    {
        var emp = new Employee(TestData.UserName("jdoe"), "John", "Doe", "john@test.com");

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(emp);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.GetEmployee(bus, emp.UserName);

        result.ShouldContain(emp.UserName);
        result.ShouldContain("John");
        result.ShouldContain("Doe");
        result.ShouldContain("john@test.com");
    }

    [Test]
    public async Task ShouldReturnNotFoundForMissingEmployee()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var result = await EmployeeTools.GetEmployee(bus, TestData.UserName("nonexistent"));

        result.ShouldContain("No employee found");
    }
}
