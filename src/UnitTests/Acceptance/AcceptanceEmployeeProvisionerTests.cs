using System.Net;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Acceptance;

[TestFixture]
public class AcceptanceEmployeeProvisionerTests
{
    [Test]
    public async Task Should_UseSqlFallback_AndNotCallCreateEmployee_When_ProbeReturns404()
    {
        var createEmployeeCalls = 0;
        var sqlFallbackCalls = 0;
        var sqlEmployee = new Employee("sql_user", "Sql", "User", "sql@example.com");
        var provisioner = new AcceptanceEmployeeProvisioner(
            () => Task.FromResult(HttpStatusCode.NotFound),
            _ =>
            {
                createEmployeeCalls++;
                return Task.FromResult(new Employee("api_user", "Api", "User", "api@example.com"));
            },
            _ =>
            {
                sqlFallbackCalls++;
                return sqlEmployee;
            });

        var provision = await provisioner.CreateAsync("tag404");

        createEmployeeCalls.ShouldBe(0);
        sqlFallbackCalls.ShouldBe(1);
        provision.TestSupportAvailable.ShouldBeFalse();
        provision.Employee.ShouldBe(sqlEmployee);
    }

    [Test]
    public async Task Should_UseTestSupportPath_When_ProbeIsNot404()
    {
        var createEmployeeCalls = 0;
        var sqlFallbackCalls = 0;
        var apiEmployee = new Employee("api_user", "Api", "User", "api@example.com");
        var provisioner = new AcceptanceEmployeeProvisioner(
            () => Task.FromResult(HttpStatusCode.OK),
            _ =>
            {
                createEmployeeCalls++;
                return Task.FromResult(apiEmployee);
            },
            _ =>
            {
                sqlFallbackCalls++;
                return new Employee("sql_user", "Sql", "User", "sql@example.com");
            });

        var provision = await provisioner.CreateAsync("tagOk");

        createEmployeeCalls.ShouldBe(1);
        sqlFallbackCalls.ShouldBe(0);
        provision.TestSupportAvailable.ShouldBeTrue();
        provision.Employee.ShouldBe(apiEmployee);
    }
}
