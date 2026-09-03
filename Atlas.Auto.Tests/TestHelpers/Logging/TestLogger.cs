using AventStack.ExtentReports;

namespace Atlas.Auto.Tests.TestHelpers.Logging;

internal interface ITestLogger
{
    void LogInfo(string message);
    void LogStart(string message);
    void LogCompletion(string message);
    void LogPass(string message);
}

internal class TestLogger : ITestLogger
{
    private readonly ExtentTest _test;

    public TestLogger(ExtentTest test)
    {
        _test = test;
    }

    public void LogInfo(string message) => _test.Info(message);

    public void LogStart(string message) => _test.Info($"Started: {message}");

    public void LogCompletion(string message) => _test.Info($"Completed: {message}");

    public void LogPass(string message) => _test.Pass(message);
}
