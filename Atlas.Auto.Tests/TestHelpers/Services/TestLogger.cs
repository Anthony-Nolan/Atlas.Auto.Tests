using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using AventStack.ExtentReports;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal interface ITestLogger
{
    /// <summary>
    /// Log informational message.
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Log informational message about the start of a test or test step.
    /// Will be prefixed with "Started: ".
    /// </summary>
    void LogStart(string message);

    /// <summary>
    /// Log informational message about the completion of a test or test step.
    /// Will be prefixed with "Completed: ".
    /// </summary>
    void LogCompletion(string message);

    /// <summary>
    /// Log a message indicating that a test or test step was successful.
    /// </summary>
    void LogPass(string message);

    /// <summary>
    /// Log a message indicating that a test or test step failed.
    /// </summary>
    void LogFailure(string message);

    /// <summary>
    /// Will <see cref="LogPass"/> if <paramref name="assertions"/> complete without error, else will <see cref="LogFailure"/> and then re-throw the exception.
    /// </summary>
    void AssertThenLogAndThrow(Action assertions, string actionDescription);

    /// <summary>
    /// Will <see cref="LogPass"/> if <paramref name="debugResponse"/> was successful, else will <see cref="LogFailure"/> and then re-throw the exception.
    /// </summary>
    void AssertResponseThenLogAndThrow<T>(DebugResponse<T> debugResponse, string actionDescription);
}

internal class TestLogger : ITestLogger
{
    private readonly ExtentTest test;

    public TestLogger(ExtentTest test)
    {
        this.test = test;
    }

    public void LogInfo(string message) => test.Info(message);

    public void LogStart(string message) => test.Info($"Started: {message}");

    public void LogCompletion(string message) => test.Info($"Completed: {message}");

    public void LogPass(string message) => test.Pass(message);

    public void LogFailure(string message) => test.Fail(message);

    public void AssertThenLogAndThrow(Action assertions, string actionDescription)
    {
        try
        {
            assertions();
            LogPass(PassMessage(actionDescription));
        }
        catch
        {
            LogFailure(FailMessage(actionDescription));
            throw;
        }
    }

    public void AssertResponseThenLogAndThrow<T>(DebugResponse<T> debugResponse, string actionDescription) =>
        AssertThenLogAndThrow(debugResponse.ShouldBeSuccessful, actionDescription);

    private static string PassMessage(string actionDescription) => $"{actionDescription} was successful";
    private static string FailMessage(string actionDescription) => $"{actionDescription} failed";
}