using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer;
using Lamar;
using MediatR;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class PaginationCapGuardTests
{
    [Test]
    public void MaxPageSize_WhenNotConfigured_IsTheDefault()
    {
        Guard(null).MaxPageSize.ShouldBe(PaginationCapGuard.DefaultMaxPageSize);
    }

    [TestCase("25", 25)]
    [TestCase("0", PaginationCapGuard.DefaultMaxPageSize)]
    [TestCase("-5", PaginationCapGuard.DefaultMaxPageSize)]
    public void MaxPageSize_WhenConfigured_UsesPositiveValuesOnly(string configured, int expected)
    {
        Guard(configured).MaxPageSize.ShouldBe(expected);
    }

    [Test]
    public void Resolve_WhenTakeIsOmitted_AppliesTheCapAsDefault()
    {
        Guard(null).Resolve(null).ShouldBe(PaginationCapGuard.DefaultMaxPageSize);
    }

    [Test]
    public void Resolve_WhenTakeIsOversize_ClampsToTheCap()
    {
        Guard("50").Resolve(10_000).ShouldBe(50);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Resolve_WhenTakeIsNotPositive_AppliesTheCap(int requested)
    {
        Guard("50").Resolve(requested).ShouldBe(50);
    }

    [Test]
    public void Resolve_WhenTakeIsWithinTheCap_KeepsIt()
    {
        Guard("50").Resolve(20).ShouldBe(20);
    }

    [Test]
    public async Task Handle_WhenQueryHasNoTake_SendsTheCappedQueryToTheHandler()
    {
        var query = new WorkOrderSpecificationQuery();
        int? takeSeenByHandler = null;

        var result = await Guard("30").Handle(query, () =>
        {
            takeSeenByHandler = query.Take;
            return Task.FromResult(Array.Empty<WorkOrder>());
        }, CancellationToken.None);

        takeSeenByHandler.ShouldBe(30);
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WhenQueryTakeIsOversize_ClampsItBeforeTheHandler()
    {
        var query = new WorkOrderSpecificationQuery { Take = 5_000 };

        await Guard(null).Handle(query, () => Task.FromResult(Array.Empty<WorkOrder>()), CancellationToken.None);

        query.Take.ShouldBe(PaginationCapGuard.DefaultMaxPageSize);
    }

    [Test]
    public void ShouldRegisterGuardAsWorkOrderListBehavior_WhenMcpServiceRegistryIsBuilt()
    {
        using var container = new Container(registry =>
        {
            registry.For<TimeProvider>().Use(TimeProvider.System);
            registry.IncludeRegistry<McpServiceRegistry>();
        });

        var behaviors = container.Model.For<IPipelineBehavior<WorkOrderSpecificationQuery, WorkOrder[]>>().Instances;

        behaviors.Count(instance => instance.ImplementationType == typeof(PaginationCapGuard)).ShouldBe(1);
    }

    private static PaginationCapGuard Guard(string? maxPageSize)
    {
        var settings = new Dictionary<string, string?>();
        if (maxPageSize != null)
        {
            settings[PaginationCapGuard.MaxPageSizeKey] = maxPageSize;
        }

        return new PaginationCapGuard(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }
}
