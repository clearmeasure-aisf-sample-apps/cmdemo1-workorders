using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Weak ETag (SHA-256 over the representation bytes) for conditional GET (<c>If-None-Match</c> → 304).
/// </summary>
public static class ConditionalGetEtag
{
    /// <summary>
    /// JSON serialization for API GET bodies: web defaults over the source-generated <see cref="ApiJsonContext"/>,
    /// the same contract the minimal API JSON results use.
    /// </summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions =
        new(JsonSerializerDefaults.Web) { TypeInfoResolver = ApiJsonContext.Default };

    /// <summary>
    /// Builds a weak entity-tag of the form <c>W/"{lowercase-hex}"</c> (SHA-256 of the representation) from UTF-8 bytes.
    /// </summary>
    private static EntityTagHeaderValue CreateWeakEtag(ReadOnlySpan<byte> representationUtf8)
    {
        var hash = SHA256.HashData(representationUtf8);
        var hex = Convert.ToHexStringLower(hash);
        return new EntityTagHeaderValue($"\"{hex}\"", isWeak: true);
    }

    /// <summary>
    /// Serializes <paramref name="value"/> to JSON (web defaults) and returns its weak ETag.
    /// </summary>
    public static EntityTagHeaderValue CreateWeakEtagForJson<T>(T value) =>
        CreateWeakEtag(JsonSerializer.SerializeToUtf8Bytes(value, TypeInfo<T>()));

    /// <summary>
    /// UTF-8 encodes <paramref name="text"/> and returns its weak ETag.
    /// </summary>
    public static EntityTagHeaderValue CreateWeakEtagForPlainText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return CreateWeakEtag(bytes);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the request's <c>If-None-Match</c> matches <paramref name="etag"/> per weak comparison.
    /// </summary>
    public static bool IfNoneMatchIncludesEtag(HttpRequest request, EntityTagHeaderValue etag)
    {
        if (!EntityTagHeaderValue.TryParseList(request.Headers.IfNoneMatch, out var candidates))
            return false;

        foreach (var candidate in candidates)
        {
            if (candidate.Equals(EntityTagHeaderValue.Any))
                return true;
            if (candidate.Compare(etag, useStrongComparison: false))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 200 OK with JSON body using <see cref="JsonSerializerOptions"/> (matches default web JSON contract).
    /// </summary>
    public static ContentHttpResult JsonContent<T>(T value) =>
        TypedResults.Content(
            JsonSerializer.Serialize(value, TypeInfo<T>()),
            "application/json; charset=utf-8",
            statusCode: StatusCodes.Status200OK);

    /// <summary>
    /// Sets the weak ETag of <paramref name="payload"/>'s JSON and answers 304 when the client already has it,
    /// otherwise 200 with the JSON.
    /// </summary>
    public static IResult ConditionalJson<T>(HttpContext context, T payload) =>
        Conditional(context, CreateWeakEtagForJson(payload), () => JsonContent(payload));

    /// <summary>
    /// Sets <paramref name="etag"/> on the response and answers 304 when the client already has it, otherwise
    /// <paramref name="representation"/>.
    /// </summary>
    public static IResult Conditional(HttpContext context, EntityTagHeaderValue etag, Func<IResult> representation)
    {
        context.Response.Headers.ETag = etag.ToString();
        return IfNoneMatchIncludesEtag(context.Request, etag)
            ? TypedResults.StatusCode(StatusCodes.Status304NotModified)
            : representation();
    }

    private static JsonTypeInfo<T> TypeInfo<T>() =>
        (JsonTypeInfo<T>)JsonSerializerOptions.GetTypeInfo(typeof(T));
}
