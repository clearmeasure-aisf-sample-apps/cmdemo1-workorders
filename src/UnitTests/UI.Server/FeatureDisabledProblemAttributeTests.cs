using ClearMeasure.Bootcamp.Core.FeatureFlags;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class FeatureDisabledProblemAttributeTests
{
    [Test]
    public void OnException_Should_Return403Problem_When_FeatureDisabled()
    {
        var context = CreateContext(new FeatureDisabledException(FeatureFlagNames.VendorManagement, "VendorsQuery"));

        new FeatureDisabledProblemAttribute().OnException(context);

        context.ExceptionHandled.ShouldBeTrue();
        var result = context.Result.ShouldBeOfType<ObjectResult>();
        result.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        result.ContentTypes.ShouldContain("application/problem+json");
        var problem = result.Value.ShouldBeOfType<ProblemDetails>();
        problem.Status.ShouldBe(StatusCodes.Status403Forbidden);
        problem.Title.ShouldBe("Feature disabled");
        problem.Type.ShouldBe(FeatureDisabledProblemAttribute.ProblemType);
        problem.Detail.ShouldNotBeNull();
        problem.Detail.ShouldContain(FeatureFlagNames.VendorManagement);
        problem.Detail.ShouldContain("VendorsQuery");
        problem.Extensions["feature"].ShouldBe(FeatureFlagNames.VendorManagement);
        problem.Instance.ShouldBe("/api/blazor-wasm-single-api");
    }

    [Test]
    public void OnException_Should_LeaveOtherExceptions_When_NotFeatureDisabled()
    {
        var context = CreateContext(new InvalidOperationException("boom"));

        new FeatureDisabledProblemAttribute().OnException(context);

        context.ExceptionHandled.ShouldBeFalse();
        context.Result.ShouldBeNull();
    }

    [Test]
    public void SingleApiController_Should_CarryFeatureDisabledProblem_When_Declared()
    {
        typeof(SingleApiController).GetCustomAttributes(typeof(FeatureDisabledProblemAttribute), true)
            .ShouldNotBeEmpty();
    }

    private static ExceptionContext CreateContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/blazor-wasm-single-api";
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ExceptionContext(actionContext, []) { Exception = exception };
    }
}
