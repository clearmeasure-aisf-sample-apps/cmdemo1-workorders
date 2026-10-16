using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class EmployeeQueryHandlerTests
{
    [Test]
    public async Task ShouldFindEmployeeByUsername()
    {
        var one = new Employee(TestData.UserName("employee1"), "first1", "last1", "email1");
        var two = new Employee(TestData.UserName("employee2"), "first2", "last2", "email2");
        var three = new Employee(TestData.UserName("employee3"), "first3", "last3", "email3");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(one);
            context.Add(two);
            context.Add(three);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeQueryHandler(dataContext);
        var employee = await handler.Handle(new EmployeeByUserNameQuery(one.UserName));
        employee.Id.ShouldBe(one.Id);
    }

    [Test]
    public async Task ShouldGetAllEmployees()
    {
        var one = new Employee(TestData.UserName("employee1"), "first1", "last1", "email1");
        var two = new Employee(TestData.UserName("employee2"), "first2", "last2", "email2");
        var three = new Employee(TestData.UserName("employee3"), "first3", "last3", "email3");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(two);
            context.Add(three);
            context.Add(one);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeQueryHandler(dataContext);
        var ownIds = new[] { one.Id, two.Id, three.Id };
        var employees = (await handler.Handle(new EmployeeGetAllQuery()))
            .Where(e => ownIds.Contains(e.Id)).ToArray();

        employees.Length.ShouldBe(3);
        employees[0].UserName.ShouldBe(one.UserName);
        employees[0].FirstName.ShouldBe("first1");
        employees[0].LastName.ShouldBe("last1");
        employees[0].EmailAddress.ShouldBe("email1");
    }

    [Test]
    public async Task ShouldPersistPreferredLanguage()
    {
        var employee = new Employee(TestData.UserName("testuser"), "Test", "User", "test@test.com") { PreferredLanguage = "fr-FR" };
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedEmployee = context.Set<Employee>().First(e => e.Id == employee.Id);
            rehydratedEmployee.PreferredLanguage.ShouldBe("fr-FR");
        }
    }

    [Test]
    public async Task ShouldPersistDefaultPreferredLanguage()
    {
        var employee = new Employee(TestData.UserName("testuser"), "Test", "User", "test@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydratedEmployee = context.Set<Employee>().First(e => e.Id == employee.Id);
            rehydratedEmployee.PreferredLanguage.ShouldBe("en-US");
        }
    }

    [Test]
    public async Task Should_ReturnStoredEmployeeNames_Unchanged_When_EmployeeGetAllQueryHandled()
    {
        var one = new Employee(TestData.UserName("employee1"), "first1", "last1", "email1");
        var two = new Employee(TestData.UserName("employee2"), "First2", "Last2", "email2");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(one);
            context.Add(two);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new EmployeeQueryHandler(dataContext);
        var employees = (await handler.Handle(new EmployeeGetAllQuery()))
            .Where(e => e.Id == one.Id || e.Id == two.Id).ToArray();

        employees.Length.ShouldBe(2);
        employees[0].FirstName.ShouldBe("first1");
        employees[0].LastName.ShouldBe("last1");
        employees[1].FirstName.ShouldBe("First2");
        employees[1].LastName.ShouldBe("Last2");
    }

    [Test]
    public async Task Should_NotMutateEmployeeRecords_When_LoginPageLoadsEmployees()
    {
        var userName = TestData.UserName("loginuser");
        var employee = new Employee(userName, "Mixed", "CaseName", "login@test.com");
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        var bus = TestHost.GetRequiredService<IBus>();
        _ = await bus.Send(new EmployeeGetAllQuery());

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Employee>().Single(e => e.UserName == userName);
            rehydrated.FirstName.ShouldBe("Mixed");
            rehydrated.LastName.ShouldBe("CaseName");
        }
    }
}
