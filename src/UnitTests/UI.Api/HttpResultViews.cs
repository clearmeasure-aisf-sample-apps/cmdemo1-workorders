using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

/// <summary>
/// Reads minimal API results the way the endpoint tests assert on them: status code, body value, plain content.
/// </summary>
internal static class HttpResultViews
{
    internal sealed record ValueView(int? StatusCode, object? Value);

    internal sealed record ContentView(int? StatusCode, string? ContentType, string? Content);

    internal sealed record StatusCodeView(int StatusCode);

    /// <summary>A 200 result carrying a JSON body value.</summary>
    internal static ValueView ShouldBeOkResult(this IResult result)
    {
        var view = result.ShouldBeValueResult();
        view.StatusCode.ShouldBe(StatusCodes.Status200OK);
        return view;
    }

    /// <summary>A result carrying a body value (a problem result carries its <c>ProblemDetails</c>).</summary>
    internal static ValueView ShouldBeValueResult(this IResult result) => result switch
    {
        ProblemHttpResult problem => new ValueView(problem.StatusCode, problem.ProblemDetails),
        IValueHttpResult value => new ValueView(((IStatusCodeHttpResult)result).StatusCode, value.Value),
        _ => throw new ShouldAssertException($"Expected a result with a body value but was {result.GetType().Name}")
    };

    /// <summary>A 400 result carrying <c>ProblemDetails</c>.</summary>
    internal static void ShouldBeBadRequestProblem(this IResult result)
    {
        var view = result.ShouldBeValueResult();
        view.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        view.Value.ShouldBeOfType<ProblemDetails>();
    }

    internal static ContentView ShouldBeContentResult(this IResult result)
    {
        var content = result.ShouldBeOfType<ContentHttpResult>();
        return new ContentView(content.StatusCode, content.ContentType, content.ResponseContent);
    }

    internal static StatusCodeView ShouldBeStatusCodeResult(this IResult result) =>
        new(result.ShouldBeOfType<StatusCodeHttpResult>().StatusCode);
}
