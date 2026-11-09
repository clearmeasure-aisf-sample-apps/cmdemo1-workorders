using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware;

[TestFixture]
public class ProblemDetailsWriterTests
{
    [Test]
    public async Task ShouldWriteAProblemJsonDocument_WhenAPlainProblemIsWritten()
    {
        var context = ContextWithBody();

        await ProblemDetailsWriter.WriteAsync(context, StatusCodes.Status409Conflict, "Conflict", "Key reused.");

        using var problem = ReadBody(context);
        context.Response.StatusCode.ShouldBe(409);
        context.Response.ContentType.ShouldBe("application/problem+json");
        problem.RootElement.GetProperty("title").GetString().ShouldBe("Conflict");
        problem.RootElement.GetProperty("detail").GetString().ShouldBe("Key reused.");
    }

    [Test]
    public async Task ShouldGroupTheMessagesByProperty_WhenValidationFailuresAreWritten()
    {
        var context = ContextWithBody();
        ValidationFailure[] failures = [new("Number", "Required."), new("Number", "Too long."), new("Title", "Required.")];

        await ProblemDetailsWriter.WriteValidationProblemAsync(context, failures);

        using var problem = ReadBody(context);
        context.Response.StatusCode.ShouldBe(400);
        problem.RootElement.GetProperty("errors").GetProperty("Number").GetArrayLength().ShouldBe(2);
        problem.RootElement.GetProperty("errors").GetProperty("Title")[0].GetString().ShouldBe("Required.");
    }

    private static DefaultHttpContext ContextWithBody() => new() { Response = { Body = new MemoryStream() } };

    private static JsonDocument ReadBody(HttpContext context) =>
        JsonDocument.Parse(((MemoryStream)context.Response.Body).ToArray());
}
