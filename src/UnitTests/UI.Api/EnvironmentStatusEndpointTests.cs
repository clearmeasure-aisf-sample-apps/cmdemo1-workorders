using System.Runtime.InteropServices;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentStatusEndpointTests
{
    private const string SecretValue = "unit-test-env-status-secret-value";

    [Test]
    public void Get_Should_ReturnJson_WithOsDescriptionProcessorCountClrVersionAndEnvVarNames()
    {
        var result = CreateEndpoint().Get();

        var payload = AssertOkPayload(result);
        payload.OsDescription.ShouldBe(RuntimeInformation.OSDescription);
        payload.ProcessorCount.ShouldBe(Environment.ProcessorCount);
        payload.ProcessorCount.ShouldBeGreaterThan(0);
        payload.ClrVersion.ShouldBe(Environment.Version.ToString());
        payload.ClrVersion.ShouldNotBeNullOrEmpty();
        payload.EnvironmentVariableNames.ShouldNotBeNull();
        payload.EnvironmentVariables.ShouldNotBeNull();
        payload.EnvironmentVariableNames.Count.ShouldBe(payload.EnvironmentVariables.Count);
    }

    [Test]
    public void Get_Should_Return304_When_IfNoneMatchMatchesPayloadEtag()
    {
        var endpoint = CreateEndpoint();
        var first = endpoint.Get();
        first.ShouldBeContentResult();
        var etag = endpoint.Response.Headers.ETag.ToString();
        etag.ShouldNotBeNullOrEmpty();

        endpoint.Request.Headers.IfNoneMatch = etag;
        var second = endpoint.Get();

        var status = second.ShouldBeStatusCodeResult();
        status.StatusCode.ShouldBe(StatusCodes.Status304NotModified);
    }

    [Test]
    public void Get_Should_OmitEnvironmentVariableValues_When_SecretEnvVarPresent()
    {
        using var probe = RedactionProbe.Install(SecretValue);

        var result = CreateEndpoint().Get();
        var payload = AssertOkPayload(result);

        payload.EnvironmentVariableNames.ShouldContain(EnvironmentStatusEndpoint.RedactionProbeVariableName);
        payload.EnvironmentVariables[EnvironmentStatusEndpoint.RedactionProbeVariableName]
            .ShouldBe(EnvironmentStatusEndpoint.RedactedValue);
        payload.EnvironmentVariables.Values.ShouldAllBe(value => value == EnvironmentStatusEndpoint.RedactedValue);
    }

    [Test]
    public void Get_Should_NotEchoSecretSubstrings_InAnyProperty()
    {
        using var probe = RedactionProbe.Install(SecretValue);

        var result = CreateEndpoint().Get();
        var content = result.ShouldBeContentResult();
        content.Content.ShouldNotBeNull();
        content.Content!.ShouldNotContain(SecretValue);
    }

    [Test]
    public void Get_Should_ReturnVersion_WhenAssemblyAttributePresent()
    {
        var result = CreateEndpoint().Get();

        var payload = AssertOkPayload(result);
        payload.Version.ShouldNotBeEmpty();
    }

    [Test]
    public void Get_Should_ReturnGitSha_WhenAssemblyMetadataAttributePresent()
    {
        var result = CreateEndpoint().Get();

        var payload = AssertOkPayload(result);
        payload.GitSha.ShouldNotBeEmpty();
    }

    private static EndpointUnderTest CreateEndpoint() => new(EnvironmentStatusEndpoint.Get);

    private sealed class EndpointUnderTest(Func<HttpContext, IResult> get)
    {
        public HttpContext HttpContext { get; } = new DefaultHttpContext();
        public HttpRequest Request => HttpContext.Request;
        public HttpResponse Response => HttpContext.Response;
        public IResult Get() => get(HttpContext);
    }

    private static EnvironmentStatusResponse AssertOkPayload(IResult result)
    {
        var content = result.ShouldBeContentResult();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        content.Content.ShouldNotBeNull();
        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload;
    }

    private sealed class RedactionProbe : IDisposable
    {
        private readonly string? _previous;

        private RedactionProbe(string? previous) => _previous = previous;

        public static RedactionProbe Install(string value)
        {
            var previous = Environment.GetEnvironmentVariable(
                EnvironmentStatusEndpoint.RedactionProbeVariableName);
            Environment.SetEnvironmentVariable(
                EnvironmentStatusEndpoint.RedactionProbeVariableName, value);
            return new RedactionProbe(previous);
        }

        public void Dispose() =>
            Environment.SetEnvironmentVariable(
                EnvironmentStatusEndpoint.RedactionProbeVariableName, _previous);
    }
}
