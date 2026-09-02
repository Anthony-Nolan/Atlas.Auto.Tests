using Atlas.Debug.Client.Models.Exceptions;
using Polly;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal static class PollyRetry
{
    public static async Task<TResult?> ExecuteWithRetry<TResult>(
        Func<Task<TResult?>> action,
        int retryCount,
        int retryIntervalInSeconds,
        string operationName) where TResult : class
    {
        var policy = Policy<TResult?>
            .HandleResult(r => r == null)
            .Or<Exception>()
            .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromSeconds(retryIntervalInSeconds),
                onRetry: (outcome, timespan, retry, _) =>
                {
                    var reason = outcome.Exception switch
                    {
                        HttpFunctionException httpEx => FormatHttpFunctionException(httpEx),
                        not null => $"{outcome.Exception.GetType().Name}: {outcome.Exception.Message}",
                        _ => "result was null"
                    };

                    TestContext.Out.WriteLineAsync(
                        $"{operationName}: {reason}. Retry {retry}/{retryCount} in {timespan.TotalSeconds}s.");
                });

        return await policy.ExecuteAsync(action);
    }

    public static async Task ExecuteWithRetry(
        Func<Task> action,
        int retryCount,
        int retryIntervalInSeconds,
        string operationName)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(retryCount, _ => TimeSpan.FromSeconds(retryIntervalInSeconds),
                onRetry: (exception, timespan, retry, _) =>
                {
                    var reason = exception switch
                    {
                        HttpFunctionException httpEx => FormatHttpFunctionException(httpEx),
                        _ => $"{exception.GetType().Name}: {exception.Message}"
                    };

                    TestContext.Out.WriteLineAsync(
                        $"{operationName}: {reason}. Retry {retry}/{retryCount} in {timespan.TotalSeconds}s.");
                });

        await policy.ExecuteAsync(action);
    }

    private static string FormatHttpFunctionException(HttpFunctionException ex)
    {
        var responseContent = ex.ResponseContent.ReadAsStringAsync().Result;
        var formattedContent = responseContent.Length > 0 ? $"{responseContent}, " : string.Empty;
        return $"HttpFunctionException: [{(int)ex.HttpStatusCode}, {ex.HttpStatusCode}] {formattedContent}{ex.Message}";
    }
}
