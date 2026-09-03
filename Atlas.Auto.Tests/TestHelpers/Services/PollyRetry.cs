using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Debug.Client.Models.Exceptions;
using Microsoft.Extensions.Logging;
using Polly;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class PollyRetry
{
    private readonly ILogger<PollyRetry> _logger;

    public PollyRetry(ILogger<PollyRetry> logger)
    {
        _logger = logger;
    }

    public async Task<TResult?> ExecuteWithRetry<TResult>(
        Func<Task<TResult?>> action,
        RetryPolicy policy,
        string operationName) where TResult : class
    {
        _logger.LogInformation("{OperationName:l} — starting (up to {RetryCount} retries, {IntervalSeconds}s interval)",
            operationName, policy.RetryCount, policy.IntervalSeconds);

        var lastRetry = 0;

        var pollyPolicy = Policy<TResult?>
            .HandleResult(r => r == null)
            .Or<Exception>()
            .WaitAndRetryAsync(policy.RetryCount, _ => TimeSpan.FromSeconds(policy.IntervalSeconds),
                onRetry: (outcome, timespan, retry, _) =>
                {
                    lastRetry = retry;
                    var reason = FormatFailureReason(outcome.Exception);
                    _logger.LogWarning("{Reason:l}. Retry {Retry}/{RetryCount} in {Delay}s",
                        reason, retry, policy.RetryCount, timespan.TotalSeconds);
                });

        var result = await pollyPolicy.ExecuteAsync(action);

        if (lastRetry == 0)
            _logger.LogInformation("{OperationName:l} — finished (no retries needed)", operationName);
        else
            _logger.LogInformation("{OperationName:l} — finished ({LastRetry} {RetryWord:l})",
                operationName, lastRetry, lastRetry == 1 ? "retry" : "retries");

        return result;
    }

    public async Task ExecuteWithRetry(
        Func<Task> action,
        RetryPolicy policy,
        string operationName)
    {
        _logger.LogInformation("{OperationName:l} — starting (up to {RetryCount} retries, {IntervalSeconds}s interval)",
            operationName, policy.RetryCount, policy.IntervalSeconds);

        var lastRetry = 0;

        var pollyPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(policy.RetryCount, _ => TimeSpan.FromSeconds(policy.IntervalSeconds),
                onRetry: (exception, timespan, retry, _) =>
                {
                    lastRetry = retry;
                    var reason = FormatFailureReason(exception);
                    _logger.LogWarning("{Reason:l}. Retry {Retry}/{RetryCount} in {Delay}s",
                        reason, retry, policy.RetryCount, timespan.TotalSeconds);
                });

        await pollyPolicy.ExecuteAsync(action);

        if (lastRetry == 0)
            _logger.LogInformation("{OperationName:l} — finished (no retries needed)", operationName);
        else
            _logger.LogInformation("{OperationName:l} — finished ({LastRetry} {RetryWord:l})",
                operationName, lastRetry, lastRetry == 1 ? "retry" : "retries");
    }

    private static string FormatFailureReason(Exception? exception)
    {
        return exception switch
        {
            HttpFunctionException httpEx => FormatHttpFunctionException(httpEx),
            not null => $"{exception.GetType().Name}: {exception.Message}",
            _ => "result was null"
        };
    }

    private static string FormatHttpFunctionException(HttpFunctionException ex)
    {
        var responseContent = ex.ResponseContent.ReadAsStringAsync().Result;
        var formattedContent = responseContent.Length > 0 ? $"{responseContent}, " : string.Empty;
        return $"HttpFunctionException: [{(int)ex.HttpStatusCode}, {ex.HttpStatusCode}] {formattedContent}{ex.Message}";
    }
}
