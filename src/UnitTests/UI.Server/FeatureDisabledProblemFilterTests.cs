using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.UI.Server.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class FeatureDisabledProblemFilterTests
{
    [Test]
    public async Task InvokeAsync_Should_Return403Problem_When_FeatureDisabled()
    {
        var result = await Invoke(new FeatureDisabledException(FeatureFlagNames.VendorManagement, "VendorsQuery"));

        var problemResult = result.ShouldBeOfType<ProblemHttpResult>();
        problemResult.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        problemResult.ContentType.ShouldBe("application/problem+json");
        var problem = problemResult.ProblemDetails;
        problem.Status.ShouldBe(StatusCodes.Status403Forbidden);
        problem.Title.ShouldBe("Feature disabled");
        problem.Type.ShouldBe(FeatureDisabledProblemFilter.ProblemType);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain(FeatureFlagNames.VendorManagement);
        problem.Detail.ShouldContain("VendorsQuery");
        problem.Extensions["feature"].ShouldBe(FeatureFlagNames.VendorManagement);
        problem.Instance.ShouldBe("/api/blazor-wasm-single-api");
    }

    [Test]
    public async Task InvokeAsync_Should_LeaveOtherExceptions_When_NotFeatureDisabled()
    {
        await Should.ThrowAsync<InvalidOperationException>(async () => await Invoke(new InvalidOperationException("boom")));
    }

    [Test]
    public async Task InvokeAsync_Should_PassResultThrough_When_EndpointSucceeds()
    {
        var context = InvocationContext();

        var result = await new FeatureDisabledProblemFilter().InvokeAsync(context, _ => ValueTask.FromResult<object?>("ok"));

        result.ShouldBe("ok");
    }

    private static async Task<object?> Invoke(Exception exception) =>
        await new FeatureDisabledProblemFilter().InvokeAsync(InvocationContext(), _ => throw exception);

    private static DefaultEndpointFilterInvocationContext InvocationContext() =>
        new(new DefaultHttpContext { Request = { Path = "/api/blazor-wasm-single-api" } });
}
