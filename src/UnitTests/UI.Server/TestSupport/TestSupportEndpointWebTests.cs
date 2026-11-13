using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.TestSupport;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

/// <summary>
/// Proves the test-support surface returns 404 outside pool slots (Enabled=false), matching tdd/uat/prod.
/// </summary>
[TestFixture]
public class TestSupportEndpointWebTests
{
    [Test]
    public async Task Should_Return404_When_TestSupportDisabled_OnNonPoolHost()
    {
        await using var factory = new TestSupportDisabledWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestSupportConstants.SecretHeaderName, "unused-when-disabled");

        var response = await client.PostAsJsonAsync(
            "/api/test-support/employees",
            new CreateEmployeeRequest(TestTag: "tag1"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Should_Return404_When_TestSupportDisabled_EvenWithoutSecret()
    {
        await using var factory = new TestSupportDisabledWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/test-support/sign-in-storage-state?userName=x");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Should_Return401_When_TestSupportEnabled_WithoutSecret()
    {
        await using var factory = new TestSupportEnabledWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/test-support/employees",
            new CreateEmployeeRequest(TestTag: "tag2"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_CreateEmployee_When_TestSupportEnabled_WithSecret()
    {
        // Shared in-memory SQLite lasts only while a connection is open.
        await using var keepDatabase = new SqliteConnection(WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString);
        await keepDatabase.OpenAsync();
        await using var factory = new TestSupportEnabledWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            TestSupportConstants.SecretHeaderName,
            TestSupportEnabledWebApplicationFactory.Secret);

        var response = await client.PostAsJsonAsync(
            "/api/test-support/employees",
            new CreateEmployeeRequest(TestTag: "abc12345"));

        var raw = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, raw);
        var body = JsonSerializer.Deserialize<CreatedEmployee>(raw, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        body.ShouldNotBeNull();
        body.UserName.ShouldStartWith("test_abc12345_");
        body.FirstName.ShouldBe("Pat");
        body.LastName.ShouldBe("Tester");
        // Display names must stay short: long unbroken First/Last tokens widen the
        // search table past the AppShell 1280px tableFits assertion.
        (body.FirstName.Length + 1 + body.LastName.Length).ShouldBeLessThanOrEqualTo(12);
    }

    [Test]
    public async Task Should_ReturnStorageState_When_SignInRequested()
    {
        await using var factory = new TestSupportEnabledWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            TestSupportConstants.SecretHeaderName,
            TestSupportEnabledWebApplicationFactory.Secret);

        var response = await client.GetAsync(
            "/api/test-support/sign-in-storage-state?userName=test_user&origin=https://localhost:7174");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain(TestSupportConstants.UserSessionStorageKey);
        json.ShouldContain("test_user");
        json.ShouldContain("https://localhost:7174");
    }

    private sealed record CreatedEmployee(string UserName, string FirstName, string LastName);
}
