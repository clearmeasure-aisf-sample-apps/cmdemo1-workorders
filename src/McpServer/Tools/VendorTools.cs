using System.ComponentModel;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using FluentValidation;
using ModelContextProtocol.Server;

namespace ClearMeasure.Bootcamp.McpServer.Tools;

[McpServerToolType]
public class VendorTools
{
    [McpServerTool(Name = "list-vendors"), Description("Lists all vendors in the system with their details.")]
    public static async Task<string> ListVendors(IBus bus)
    {
        var vendors = await bus.Send(new VendorsQuery());
        return JsonSerializer.Serialize(vendors.Select(FormatVendor).ToArray(),
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool(Name = "create-vendor"), Description("Creates a new vendor.")]
    public static async Task<string> CreateVendor(
        IBus bus,
        [Description("The name of the vendor")] string name,
        [Description("The type of work (e.g., HVAC, plumbing)")] string trade,
        [Description("Optional name of the contact person")] string? contactName = null,
        [Description("Optional email address")] string? emailAddress = null,
        [Description("Optional phone number")] string? phoneNumber = null,
        [Description("Whether the vendor is currently active")] bool isActive = true)
    {
        try
        {
            var command = new CreateVendorCommand(name, trade, contactName, emailAddress, phoneNumber, isActive);
            var result = await bus.Send(command);

            if (result.Succeeded)
            {
                var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Serialize(new
                {
                    result.Succeeded,
                    result.Message,
                    result.Id
                }, options);
            }

            return result.Message;
        }
        catch (ValidationException ex)
        {
            return ex.Message;
        }
    }

    private static object FormatVendor(Vendor vendor) => new
    {
        vendor.Id,
        vendor.Name,
        vendor.Trade,
        vendor.ContactName,
        vendor.EmailAddress,
        vendor.PhoneNumber,
        vendor.IsActive
    };
}
