using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class DatedWorkOrderBatchCreatorTests
{
    private string _dbPath = null!;
    private DataContext _context = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        _context = new DataContext(new FileDbConfig($"Data Source={_dbPath}"));
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task CreateAsync_ShouldFail_WhenAssigneeIsMissing()
    {
        var creator = EmployeeBuilder.Create("creator1").WithFirstName("C").WithLastName("One")
            .WithEmail("c@test.com").Build();
        _context.Add(creator);
        await _context.SaveChangesAsync();

        var result = await new DatedWorkOrderBatchCreator(
                _context, new StubBus(), new StubNumbers(), TimeProvider.System)
            .CreateAsync(
                new CreateDatedWorkOrdersCommand("creator1", "missing", "Title", "Desc",
                    [new DateOnly(2026, 8, 29)]),
                NullLogger.Instance,
                CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Assignee");
        result.WorkOrders.Count.ShouldBe(0);
    }

    private sealed class StubBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            throw new NotSupportedException();

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }

    private sealed class StubNumbers : IWorkOrderNumberGenerator
    {
        public string GenerateNumber() => "WO12345";
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool()
        {
        }
    }
}
