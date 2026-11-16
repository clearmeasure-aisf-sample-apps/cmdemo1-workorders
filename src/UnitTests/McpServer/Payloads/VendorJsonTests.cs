using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer.Payloads;

[TestFixture]
public class VendorJsonTests
{
    [Test]
    public void From_Should_CopyContactDetailsAndActiveFlag()
    {
        var vendor = new Vendor { Name = "Acme Plumbing", Trade = "Plumbing", PhoneNumber = "555-0100", IsActive = true };

        var json = VendorJson.From(vendor);

        json.ShouldBe(new VendorJson
        {
            Id = vendor.Id, Name = "Acme Plumbing", Trade = "Plumbing", PhoneNumber = "555-0100", IsActive = true
        });
    }
}
