using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI.Server;

[TestFixture]
public class ServerMessagingEndpointTests
{
    [TestCase("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=test")]
    [TestCase("data source=ChurchBulletin.db")]
    public void ShouldUseTheLearningTransport_WhenTheDatabaseIsAFileDatabase(string connectionString)
    {
        var learning = ServerMessagingEndpoint.ShouldUseLearningTransport(connectionString);

        learning.ShouldBeTrue();
    }

    [TestCase("server=localhost,1433;database=test")]
    [TestCase("")]
    [TestCase(null)]
    public void ShouldUseTheSqlServerTransport_WhenTheDatabaseIsSqlServerOrUnset(string? connectionString)
    {
        var learning = ServerMessagingEndpoint.ShouldUseLearningTransport(connectionString);

        learning.ShouldBeFalse();
    }
}
