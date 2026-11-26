using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

internal sealed class NextRecorder
{
    public int Calls { get; private set; }

    public Task Invoke(HttpContext context)
    {
        Calls++;
        return Task.CompletedTask;
    }
}
