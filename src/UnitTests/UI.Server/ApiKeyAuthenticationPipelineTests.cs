using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ApiKeyAuthenticationPipelineTests
{
    [Test]
    public async Task Should_SkipValidation_When_PathIsPublic()
    {
        var context = new DefaultHttpContext { Request = { Path = "/api/version" } };
        var nextCalls = 0;

        await ApiKeyAuthenticationPipeline.InvokeAsync(
            context,
            next =>
            {
                nextCalls++;
                next.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "secret" });

        nextCalls.ShouldBe(1);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Test]
    public async Task Should_Return401_When_KeyIsMissing()
    {
        var context = new DefaultHttpContext { Request = { Path = "/api/work-orders/bulk-import" } };
        var nextCalls = 0;

        await ApiKeyAuthenticationPipeline.InvokeAsync(
            context,
            next =>
            {
                nextCalls++;
                next.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "secret" });

        nextCalls.ShouldBe(0);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task Should_Return401_When_KeyIsWrong()
    {
        var context = new DefaultHttpContext { Request = { Path = "/api/work-orders/bulk-import", Headers = { ["X-Api-Key"] = "wrong" } } };
        var nextCalls = 0;

        await ApiKeyAuthenticationPipeline.InvokeAsync(
            context,
            next =>
            {
                nextCalls++;
                next.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "secret" });

        nextCalls.ShouldBe(0);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
    }

    [Test]
    public async Task Should_Return200_When_KeyMatches()
    {
        var context = new DefaultHttpContext { Request = { Path = "/api/work-orders/bulk-import", Headers = { ["X-Api-Key"] = "secret" } } };
        var nextCalls = 0;

        await ApiKeyAuthenticationPipeline.InvokeAsync(
            context,
            next =>
            {
                nextCalls++;
                next.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "secret" });

        nextCalls.ShouldBe(1);
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }
}
