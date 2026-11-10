using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Middleware.Idempotency;

/// <summary>
/// HTTP requests and responses for the idempotency fixtures.
/// </summary>
public static class IdempotencyMother
{
    public static HttpRequest Request(string method, string path)
    {
        var request = new DefaultHttpContext().Request;
        request.Method = method;
        request.Path = path;
        return request;
    }

    public static HttpRequest Post(string body)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(body);
        var request = Request("POST", "/api/test");
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
        return request;
    }

    public static HttpContext ContextWithBufferedResponse()
    {
        return new DefaultHttpContext { Response = { Body = new MemoryStream() } };
    }
}
