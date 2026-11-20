using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>UI.Server host with API key validation off, for asserting the retired sample endpoints are gone.</summary>
public sealed class RetiredSampleEndpointsWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.Timeout = TestServerClientTimeout.Value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", "Data Source=:memory:");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = ""
            });
        });
    }
}
