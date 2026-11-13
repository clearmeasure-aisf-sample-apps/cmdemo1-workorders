using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.TestSupport;

/// <summary>
/// In-process UI.Server host with TestSupport left off — the default for tdd, uat and prod (non-pool).
/// </summary>
public sealed class TestSupportDisabledWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["TestSupport:Enabled"] = "false",
                ["TestSupport:Secret"] = "unused-when-disabled"
            });
        });
    }
}
