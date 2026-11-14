using ClearMeasure.Bootcamp.DataAccess.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.UI.Server.TestSupport;

/// <summary>
/// Minimal API surface for pool-slot acceptance data. Gated by <see cref="TestSupportMiddleware"/>.
/// </summary>
public static class TestSupportEndpoints
{
    /// <summary>Maps employee and work-order create routes under <see cref="TestSupportConstants.PathPrefix"/>.</summary>
    public static void MapTestSupportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(TestSupportConstants.PathPrefix);
        group.MapPost("/employees", CreateEmployeeAsync);
        group.MapPost("/work-orders", CreateWorkOrderAsync);
        group.MapGet("/sign-in-storage-state", BuildSignInStorageState);
    }

    private static async Task<IResult> CreateEmployeeAsync(
        [FromBody] CreateEmployeeRequest request,
        TestSupportDataService data,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TestTag))
        {
            return Results.Problem(detail: "TestTag is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var employee = await data.CreateEmployeeAsync(ToEmployeeCreate(request), cancellationToken);
        return Results.Json(employee.ToApiShape());
    }

    private static async Task<IResult> CreateWorkOrderAsync(
        [FromBody] CreateWorkOrderRequest request,
        TestSupportDataService data,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TestTag) || string.IsNullOrWhiteSpace(request.CreatorUserName))
        {
            return Results.Problem(
                detail: "TestTag and CreatorUserName are required.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var workOrder = await data.CreateWorkOrderAsync(ToWorkOrderCreate(request), cancellationToken);
        return Results.Json(workOrder.ToApiShape());
    }

    private static TestSupportEmployeeCreate ToEmployeeCreate(CreateEmployeeRequest request) => new()
    {
        TestTag = request.TestTag.Trim(),
        CanCreateWorkOrder = request.CanCreateWorkOrder,
        CanFulfillWorkOrder = request.CanFulfillWorkOrder,
        FirstName = request.FirstName,
        LastName = request.LastName,
        MiddleName = request.MiddleName,
        AdditionalCreateRoleNames = request.AdditionalCreateRoleNames
    };

    private static TestSupportWorkOrderCreate ToWorkOrderCreate(CreateWorkOrderRequest request) => new()
    {
        TestTag = request.TestTag.Trim(),
        CreatorUserName = request.CreatorUserName.Trim(),
        Title = request.Title,
        Description = request.Description,
        RoomNumber = request.RoomNumber,
        Instructions = request.Instructions,
        Number = request.Number,
        StatusKey = request.StatusKey,
        Priority = request.Priority,
        DueDate = request.DueDate,
        AssigneeUserName = request.AssigneeUserName,
        Building = request.Building,
        CreatedDate = request.CreatedDate,
        CompletedDate = request.CompletedDate
    };

    private static IResult BuildSignInStorageState(
        [FromQuery] string userName,
        [FromQuery] string origin,
        HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Results.Problem(detail: "userName is required.", statusCode: StatusCodes.Status400BadRequest);
        }

        var resolvedOrigin = string.IsNullOrWhiteSpace(origin)
            ? $"{request.Scheme}://{request.Host}"
            : origin.Trim().TrimEnd('/');
        var state = PlaywrightStorageStateFactory.ForUser(userName.Trim(), resolvedOrigin);
        return Results.Json(PlaywrightStorageStateFactory.ToSerializable(state));
    }
}
