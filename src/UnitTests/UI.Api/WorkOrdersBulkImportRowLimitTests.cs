using System.Text;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Import;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.TestSupport;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WorkOrdersBulkImportRowLimitTests
{
    private const string Header = "Title,Description,CreatorUsername\n";

    [Test]
    public async Task ShouldReturnBadRequestProblemNamingMax_WhenRowsExceedLimit()
    {
        var bus = new StubBus();
        var controller = CreateController(bus, new CsvImportRowLimitGuard(2));

        var result = await controller.Post(CreateFormFile(Header + Rows(3)), CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var problem = objectResult.Value.ShouldBeOfType<ProblemDetails>();
        problem.Detail.ShouldBe(new CsvImportRowLimitGuard(2).OverLimitMessage);
        problem.Detail!.ShouldContain("2");
        bus.SaveDraftCalls.ShouldBe(0);
        bus.EmployeeLookupCalls.ShouldBe(0);
    }

    [Test]
    public async Task ShouldImportEveryRow_WhenRowsAtLimit()
    {
        var bus = new StubBus();
        var controller = CreateController(bus, new CsvImportRowLimitGuard(2));

        var result = await controller.Post(CreateFormFile(Header + Rows(2)), CancellationToken.None);

        var payload = result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<WorkOrderBulkImportResponse>();
        payload.CreatedCount.ShouldBe(2);
        bus.SaveDraftCalls.ShouldBe(2);
    }

    [Test]
    public async Task ShouldApplyDefaultLimit_WhenNoGuardSupplied()
    {
        var bus = new StubBus();
        var controller = CreateController(bus, null);

        var result = await controller.Post(
            CreateFormFile(Header + Rows(CsvImportRowLimitGuard.DefaultMaxRows + 1)), CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        objectResult.Value.ShouldBeOfType<ProblemDetails>().Detail.ShouldBe(CsvImportRowLimitGuard.Default.OverLimitMessage);
        bus.SaveDraftCalls.ShouldBe(0);
    }

    [Test]
    public async Task ShouldStillRejectHeaderOnlyCsv_WhenGuardSupplied()
    {
        var controller = CreateController(new StubBus(), new CsvImportRowLimitGuard(2));

        var result = await controller.Post(CreateFormFile(Header), CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        objectResult.Value.ShouldBeOfType<ProblemDetails>().Detail.ShouldBe("CSV contains no data rows.");
    }

    private static WorkOrdersBulkImportController CreateController(StubBus bus, CsvImportRowLimitGuard? guard)
    {
        var services = new ServiceCollection().AddMvcCore().Services.BuildServiceProvider();
        return new WorkOrdersBulkImportController(bus, new StubNumberGenerator(), guard)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } },
            ProblemDetailsFactory = services.GetRequiredService<ProblemDetailsFactory>()
        };
    }

    private static FormFile CreateFormFile(string csvContent)
    {
        var bytes = Encoding.UTF8.GetBytes(csvContent);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "import.csv")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/csv"
        };
    }

    private static string Rows(int count) =>
        string.Concat(Enumerable.Range(1, count).Select(i => $"T{i},D{i},u1\n"));

    private sealed class StubNumberGenerator : IWorkOrderNumberGenerator
    {
        public string GenerateNumber() => "WO-1";
    }

    private sealed class StubBus : IBus
    {
        private readonly Employee _creator = EmployeeBuilder.Create("u1").Build();

        public int SaveDraftCalls { get; private set; }

        public int EmployeeLookupCalls { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            switch (request)
            {
                case EmployeeByUserNameQuery:
                    EmployeeLookupCalls++;
                    return Task.FromResult((TResponse)(object)_creator);
                case SaveDraftCommand command:
                    SaveDraftCalls++;
                    return Task.FromResult((TResponse)(object)new StateCommandResult(command.WorkOrder, "Save", "ok"));
                default:
                    throw new NotSupportedException(request.GetType().FullName);
            }
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => throw new NotImplementedException();
    }
}
