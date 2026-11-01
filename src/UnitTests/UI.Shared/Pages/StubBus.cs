using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class StubBus(WorkOrder[]? workOrderResults = null) : Bus(null!)
{
    public int SendCallCount { get; private set; }

    public WorkOrderSearchSummary Summary { get; set; } = new();

    public int SummaryRequestCount { get; private set; }

    public string? LastSummaryUserName { get; private set; }

    public Dictionary<string, int>? StatusCounts { get; init; }

    public WorkOrder[] RecentWorkOrders { get; init; } = [];

    public WorkOrderSpecificationQuery? LastSpecification { get; private set; }

    public List<SavedSearchView> SavedViews { get; } = [];

    public SaveSearchViewCommand? LastSaveCommand { get; private set; }

    public DeleteSearchViewCommand? LastDeleteCommand { get; private set; }

    public SavedSearchViewResult SaveResult { get; init; } = SavedSearchViewResult.Success;

    public bool FailSavedViewsQuery { get; init; }

    public bool ThrowOnSaveCommand { get; init; }

    public bool ThrowOnDeleteCommand { get; init; }

    public override Task Publish(INotification notification)
    {
        return Task.CompletedTask;
    }

    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        SendCallCount++;
        if (request is EmployeeGetAllQuery)
        {
            return (Task<TResponse>)EmployeeGetAllQueryResponse<TResponse>();
        }

        if (request is EmployeeByUserNameQuery)
        {
            return (Task<TResponse>)EmployeeByUserNameQueryResponse<TResponse>();
        }

        if (request is WorkOrderSpecificationQuery specification)
        {
            LastSpecification = specification;
            return Task.FromResult<TResponse>((TResponse)(object)(workOrderResults ?? WorkOrderSpecificationQueryResponse()));
        }

        if (request is WorkOrderAttachmentsQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderAttachment>());
        }

        if (request is WorkOrderByNumberQuery)
        {
            var workOrder = new WorkOrder
            {
                Id = Guid.NewGuid(),
                Number = "WO-001",
                Title = "Fix broken door",
                Status = WorkOrderStatus.Draft,
                Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
            };
            return Task.FromResult<TResponse>((TResponse)(object)workOrder);
        }

        if (request is WorkOrderCountByStatusQuery)
        {
            var counts = StatusCounts ?? WorkOrderStatus.GetAllItems().ToDictionary(s => s.Key, _ => 0);
            return Task.FromResult<TResponse>((TResponse)(object)counts);
        }

        if (request is WorkOrderSearchSummaryQuery summaryQuery)
        {
            SummaryRequestCount++;
            LastSummaryUserName = summaryQuery.UserName;
            return Task.FromResult<TResponse>((TResponse)(object)Summary);
        }

        if (request is SavedSearchViewsQuery)
        {
            if (FailSavedViewsQuery)
            {
                throw new InvalidOperationException("Invalid object name 'dbo.SavedSearchView'.");
            }

            return Task.FromResult<TResponse>((TResponse)(object)SavedViews.ToArray());
        }

        if (request is SaveSearchViewCommand save)
        {
            LastSaveCommand = save;
            if (ThrowOnSaveCommand) throw new InvalidOperationException("Invalid object name 'dbo.SavedSearchView'.");
            if (SaveResult.Succeeded)
            {
                SavedViews.Add(new SavedSearchView { Name = save.Name, Query = save.Query });
            }

            return Task.FromResult<TResponse>((TResponse)(object)SaveResult);
        }

        if (request is DeleteSearchViewCommand delete)
        {
            LastDeleteCommand = delete;
            if (ThrowOnDeleteCommand) throw new InvalidOperationException("Invalid object name 'dbo.SavedSearchView'.");
            SavedViews.RemoveAll(v => v.Id == delete.Id);
            return Task.FromResult<TResponse>((TResponse)(object)SavedSearchViewResult.Success);
        }

        if (request is RecentWorkOrdersQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)RecentWorkOrders);
        }

        if (request is WorkOrderBuildingsQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)new[] { "Kitchen", "Sanctuary" });
        }

        if (request is WorkOrderReportsQuery)
        {
            var data = new WorkOrderReportsData
            {
                CountByStatus = new() { { "Draft", 2 }, { "Assigned", 3 }, { "InProgress", 1 } },
                CountByPriority = new() { { "High", 2 }, { "Medium", 3 }, { "Low", 1 } },
                CountByBuilding = new() { { "Sanctuary", 4 }, { "Kitchen", 2 } },
                CompletedPerWeek =
                [
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-49)), Count = 1 },
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-42)), Count = 2 },
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-35)), Count = 1 },
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-28)), Count = 3 },
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-21)), Count = 0 },
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), Count = 2 },
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), Count = 1 },
                    new() { WeekEndingDate = DateOnly.FromDateTime(DateTime.UtcNow), Count = 2 }
                ],
                AverageDaysToClose = 2.5,
                OverdueCount = 1
            };
            return Task.FromResult<TResponse>((TResponse)(object)data);
        }

        if (request is EmployeeWorkLoadQuery)
        {
            var load = new EmployeeWorkLoad { AssignedAndOpen = 5, InProgress = 2, Overdue = 1, CompletedLast30Days = 10 };
            return Task.FromResult<TResponse>((TResponse)(object)load);
        }

        if (request is SlaHealthQuery)
        {
            var slaHealth = new SlaHealth { CurrentMonthPercentage = null, LastMonthPercentage = null, CurrentMonthOrderCount = 0, LastMonthOrderCount = 0 };
            return Task.FromResult<TResponse>((TResponse)(object)slaHealth);
        }

        if (request is WorkOrderStatusHistoryQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkOrderStatusChange>());
        }

        throw new NotImplementedException();
    }

    private static Func<WorkOrder[]> WorkOrderSpecificationQueryResponse => () =>
    [
        new WorkOrder
        {
            Number = "WO-001",
            Title = "Fix broken door",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com"),
            Assignee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com")
        },
        new WorkOrder
        {
            Number = "WO-002",
            Title = "Replace light bulb",
            Status = WorkOrderStatus.Assigned,
            Creator = new Employee("mburns", "Montgomery", "Burns", "burns@example.com"),
            Assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
        }
    ];

    private static Task EmployeeByUserNameQueryResponse<TResponse>()
    {
        var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com");
        return Task.FromResult<TResponse>((TResponse)(object)employee);
    }

    private Task EmployeeGetAllQueryResponse<TResponse>()
    {
        var employees = new[]
        {
            new Employee("hsimpson", "HOMER", "SIMPSON", "homer@springfield.com"),
            new Employee("mburns", "Montgomery", "Burns", "burns@plant.com"),
            new Employee("nflanders", "Ned", "Flanders", "ned@flanders.com"),
            new Employee("jdoe", "mary jane", "SIMPSON", "mj@test.com"),
            new Employee("tlovejoy", "Timothy", "Lovejoy Jr", "reverend@firstchurchspringfield.org")
        };
        return Task.FromResult<TResponse>((TResponse)(object)employees);
    }
}

public class StubBusWithAssigneeCapture : StubBus
{
    public string? LastAssigneeQueried { get; private set; }

    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is EmployeeByUserNameQuery q)
        {
            LastAssigneeQueried = q.Username;
        }

        return base.Send(request);
    }
}
