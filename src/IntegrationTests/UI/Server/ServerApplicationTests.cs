using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI.Server;

[TestFixture]
public class ServerApplicationTests
{
    [Test]
    public void ShouldBuildApplicationWithoutThrowing()
    {
        var app = ServerApplication.BuildApplication([], builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
            });
            builder.Environment.EnvironmentName = "Testing";
        });

        app.ShouldNotBeNull();
    }
}
