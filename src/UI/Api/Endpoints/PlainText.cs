using Microsoft.AspNetCore.Http.HttpResults;

namespace ClearMeasure.Bootcamp.UI.Api.Endpoints;

/// <summary>200 OK plain-text (UTF-8) responses.</summary>
public static class PlainText
{
    public static ContentHttpResult Ok(string text) =>
        TypedResults.Content(text, "text/plain; charset=utf-8", statusCode: StatusCodes.Status200OK);
}
