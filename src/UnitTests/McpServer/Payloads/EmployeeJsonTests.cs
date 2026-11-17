using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

/// <summary>The employee shape MCP tools answer with, written through the source-generated <see cref="McpJsonContext"/>.</summary>
[TestFixture]
public class EmployeeJsonTests
{
    [Test]
    public void From_Should_CopyNamesEmailAndRoles()
    {
        var employee = EmployeeBuilder.Create("jpalermo").WithFirstName("Jeffrey").WithLastName("Palermo")
            .WithEmail("jeffrey@example.test").WithRole("Creator", canCreate: true, canFulfill: false).Build();

        var json = EmployeeJson.From(employee);

        json.UserName.ShouldBe("jpalermo");
        json.FirstName.ShouldBe("Jeffrey");
        json.EmailAddress.ShouldBe("jeffrey@example.test");
        json.Roles.ShouldHaveSingleItem().Name.ShouldBe("Creator");
    }

    [Test]
    public void Serialize_Should_WriteUserNameFirst_InPascalCase()
    {
        var employee = EmployeeBuilder.Create("jpalermo").Build();

        var json = JsonSerializer.Serialize(EmployeeJson.From(employee), McpJsonContext.Default.EmployeeJson);

        json.ShouldStartWith("{" + Environment.NewLine + "  \"UserName\": \"jpalermo\"");
    }
}
