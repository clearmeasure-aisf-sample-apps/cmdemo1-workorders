// ReSharper disable MethodHasAsyncOverload -- Qodana P5 (#9440): synchronous TestContext.Out.WriteLine diagnostics/setup are intentional

using ClearMeasure.Bootcamp.AcceptanceTests.Builders;
using ClearMeasure.Bootcamp.Core.Model.Messages;
using NServiceBus;

namespace ClearMeasure.Bootcamp.AcceptanceTests.NServiceBus;

/// <summary>
/// Tracer bullet acceptance test that proves the full NServiceBus Send/Reply pipeline
/// between the test endpoint ("IntegrationTests") and the Worker endpoint ("WorkOrderProcessing").
/// Correlation id is unique per run so the fixture is safe under <see cref="ParallelScope.All"/>.
/// </summary>
[TestFixture]
public class TracerBulletTests : AcceptanceApiTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task TracerBullet_WorkerReceivesCommandAndReplies()
    {
        if (!ServerFixture.WorkerStarted)
        {
            Assert.Ignore("Worker is not running (requires SqlServerTransport). Skipping tracer bullet test.");
        }

        var correlationId = Guid.NewGuid();
        var messageSession = TestHost.GetRequiredService<IMessageSession>(newScope: false);
        var replyTask = TracerBulletSignal.WaitForReply(correlationId, TimeSpan.FromSeconds(60));

        await messageSession.Send(new TracerBulletCommand(correlationId));
        TestContext.Out.WriteLine($"TracerBulletCommand sent with CorrelationId={correlationId}");

        await replyTask;
        TestContext.Out.WriteLine($"TracerBulletReplyMessage received for CorrelationId={correlationId}");
    }
}
