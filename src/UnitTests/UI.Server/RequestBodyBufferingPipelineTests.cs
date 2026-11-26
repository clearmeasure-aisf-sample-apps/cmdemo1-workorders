using System.IO.Pipelines;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestBodyBufferingPipelineTests
{
    [Test]
    public async Task Should_MakeBodySeekable_When_BufferingEnabledForPostWithBody()
    {
        var context = CreatePostContext(options => options.BufferThreshold = 1024);
        var next = new NextRecorder();

        await RequestBodyBufferingPipeline.InvokeAsync(context, next.Invoke);

        context.Request.Body.CanSeek.ShouldBeTrue();
        next.Calls.ShouldBe(1);
    }

    [Test]
    public async Task Should_ClampThreshold_When_BufferThresholdExceedsIntRange()
    {
        var context = CreatePostContext(options => options.BufferThreshold = long.MaxValue);

        await RequestBodyBufferingPipeline.InvokeAsync(context, _ => Task.CompletedTask);

        context.Request.Body.CanSeek.ShouldBeTrue();
    }

    [Test]
    public async Task Should_LeaveBodyUntouched_When_BufferingDisabled()
    {
        var context = CreatePostContext(options => options.Enabled = false);
        var originalBody = context.Request.Body;

        await RequestBodyBufferingPipeline.InvokeAsync(context, _ => Task.CompletedTask);

        context.Request.Body.ShouldBeSameAs(originalBody);
        context.Request.Body.CanSeek.ShouldBeFalse();
    }

    private static DefaultHttpContext CreatePostContext(Action<RequestBodyBufferingOptions> configure) =>
        new()
        {
            RequestServices = new ServiceCollection().Configure(configure).BuildServiceProvider(),
            Request =
            {
                Method = HttpMethods.Post,
                ContentLength = 3,
                Body = PipeReader.Create(new MemoryStream("abc"u8.ToArray())).AsStream()
            }
        };
}
