using Serilog.Core;
using Serilog.Events;

namespace Atlas.Auto.Tests.TestHelpers.Logging;

internal class NUnitSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        TestContext.Out.WriteLine($"[{logEvent.Timestamp:HH:mm:ss}] [{logEvent.Level.ToString().ToUpperInvariant()[..3]}] {message}");

        if (logEvent.Exception != null)
            TestContext.Out.WriteLine(logEvent.Exception.ToString());
    }
}
