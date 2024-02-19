using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Polly;

namespace Atlas.Auto.Tests.TestHelpers.Services;

public interface IDebugRequester
{
    /// <summary>
    /// Wraps a <paramref name="debugRequest"/> in a retry loop that will retry the request if unsuccessful.
    /// After each failed attempt, the request will wait for a period of <paramref name="retryIntervalInSeconds"/> before retrying.
    /// Exceptions are caught and treated as a failed request.
    /// </summary>
    /// <returns></returns>
    Task<DebugResponse<TResult>> ExecuteDebugRequestWithWaitAndRetry<TResult>(
        int retryCount, int retryIntervalInSeconds, Func<Task<DebugResponse<TResult>>> debugRequest);

    /// <summary>
    /// <inheritdoc cref="ExecuteDebugRequestWithWaitAndRetry{T}"/>
    /// This method is for a <paramref name="debugRequest"/> that does not return a result object, only `true` on success and `false` on failure.
    /// </summary>
    /// <returns></returns>
    Task<bool> ExecuteDebugRequestWithWaitAndRetry(
        int retryCount, int retryIntervalInSeconds, Func<Task<bool>> debugRequest);
}

internal class DebugRequester : IDebugRequester
{
    /// <inheritdoc />
    public async Task<DebugResponse<TResult>> ExecuteDebugRequestWithWaitAndRetry<TResult>(
        int retryCount,
        int retryIntervalInSeconds,
        Func<Task<DebugResponse<TResult>>> debugRequest)
    {
        return await ExecuteDebugRequestWithWaitAndRetry(
            retryCount,
            retryIntervalInSeconds,
            debugRequest,
            response => !response.WasSuccess,
            () => new DebugResponse<TResult>());
    }

    /// <inheritdoc />
    public Task<bool> ExecuteDebugRequestWithWaitAndRetry(
        int retryCount,
        int retryIntervalInSeconds,
        Func<Task<bool>> debugRequest)
    {
        return ExecuteDebugRequestWithWaitAndRetry(
            retryCount,
            retryIntervalInSeconds,
            debugRequest,
            response => !response,
            () => false);
    }

    private static async Task<TResponse> ExecuteDebugRequestWithWaitAndRetry<TResponse>(
        int retryCount,
        int retryIntervalInSeconds,
        Func<Task<TResponse>> debugRequest,
        Func<TResponse, bool> failureCheck,
        Func<TResponse> failureResponseFactory)
    {
        var policy = Policy
            .HandleResult(failureCheck)
            .WaitAndRetryAsync(retryCount, attempt => TimeSpan.FromSeconds(retryIntervalInSeconds));

        async Task<TResponse> WrappedRequest()
        {
            try
            {
                //todo #1: log request attempt
                return await debugRequest();
            }
            catch (Exception)
            {
                //todo #1: log exception
                return failureResponseFactory();
            }
        }

        return await policy.ExecuteAsync(WrappedRequest);
    }
}