using System.Net;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class WorkOrdersBulkImportIntegrationTests
{
    private SqliteConnection? _sharedMemoryHold;
    private WorkOrdersBulkImportWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _sharedMemoryHold = new SqliteConnection(WorkOrdersBulkImportWebApplicationFactory.SqliteConnectionString);
        _sharedMemoryHold.Open();
        _factory = new WorkOrdersBulkImportWebApplicationFactory();
        _httpClient = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        _sharedMemoryHold?.Dispose();
    }

    [Test]
    public async Task ShouldCreateDrafts_WhenCsvUploaded()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            await db.Database.EnsureCreatedAsync();
            var creator = new Employee("bulk-user", "Bulk", "User", "bulk@t.test");
            db.Add(creator);
            await db.SaveChangesAsync();
        }

        var csv = "Title,Description,CreatorUsername,RoomNumber\n"
                  + "First,Desc one,bulk-user,1A\n"
                  + "Second,Desc two,bulk-user,\n";

        using var content = new MultipartFormDataContent();
        var filePart = new StringContent(csv, Encoding.UTF8, "text/csv");
        content.Add(filePart, "file", "orders.csv");

        var response = await _httpClient!.PostAsync(new Uri(_httpClient.BaseAddress!, "api/v1.0/work-orders/bulk-import"), content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("createdCount").GetInt32().ShouldBe(2);
        root.GetProperty("results").GetArrayLength().ShouldBe(2);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<DataContext>();
        var count = await db2.Set<WorkOrder>().CountAsync(w => w.Creator!.UserName == "bulk-user");
        count.ShouldBe(2);
    }

    [Test]
    public async Task ShouldReturn400_WhenCreatorMissing()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            await db.Database.EnsureCreatedAsync();
        }

        var csv = "Title,Description,CreatorUsername\nT,D,nobody\n";
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8, "text/csv"), "file", "x.csv");

        var response = await _httpClient!.PostAsync(new Uri(_httpClient.BaseAddress!, "api/v1.0/work-orders/bulk-import"), content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("\"success\":false");
        json.ShouldContain("createdCount\":0");
    }
    [Test]
    public async Task ShouldImportBuildingPriorityAndDueDate_WhenSevenColumnCsvUploaded()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            await db.Database.EnsureCreatedAsync();
            db.Add(new Employee("bulk-seven", "Bulk", "Seven", "seven@t.test"));
            await db.SaveChangesAsync();
        }

        var csv = "Title,Description,CreatorUsername,RoomNumber,Building,Priority,DueDate\n"
                  + "Seven A,Desc A,bulk-seven,2B,East Wing,high,2026-12-01\n"
                  + "Seven B,Desc B,bulk-seven,,,,\n";
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8, "text/csv"), "file", "seven.csv");

        var response = await _httpClient!.PostAsync(new Uri(_httpClient.BaseAddress!, "api/v1.0/work-orders/bulk-import"), content);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<DataContext>();
        var orders = await db2.Set<WorkOrder>().Where(w => w.Creator!.UserName == "bulk-seven").ToListAsync();
        orders.Count.ShouldBe(2);
        var a = orders.Single(w => w.Title == "Seven A");
        a.Building.ShouldBe("East Wing");
        a.Priority.ShouldBe(WorkOrderPriority.High);
        a.DueDate.ShouldBe(new DateOnly(2026, 12, 1));
        var b = orders.Single(w => w.Title == "Seven B");
        b.Building.ShouldBeNull();
        b.Priority.ShouldBe(WorkOrderPriority.Medium);
        b.DueDate.ShouldBeNull();
    }
}
