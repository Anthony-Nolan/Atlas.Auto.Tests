using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Models.Exceptions;
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
        var retryMessage = $"Retrying {debugRequest.Method.Name} in {retryIntervalInSeconds}s.";

        var policy = Policy
            .HandleResult(failureCheck)
            .WaitAndRetryAsync(retryCount, attempt => TimeSpan.FromSeconds(retryIntervalInSeconds));

        async Task<TResponse> WrappedRequest()
        {
            try
            {
                await TestContext.Out.WriteLineAsync($"Executing debug request: {debugRequest.Method.Name}");
                return await debugRequest();
            }
            catch (HttpFunctionException ex)
            {
                await TestContext.Out.WriteLineAsync($"{BuildHttpFunctionExceptionMessage(ex)}. {retryMessage}");
                return failureResponseFactory();
            }
            catch (Exception ex)
            {
                await TestContext.Out.WriteLineAsync($"{ex.GetType().Name}: {ex.Message}. {retryMessage}");
                return failureResponseFactory();
            }
        }

        return await policy.ExecuteAsync(WrappedRequest);
    }

    private static string BuildHttpFunctionExceptionMessage(HttpFunctionException ex)
    {
        var responseContent = ex.ResponseContent.ReadAsStringAsync().Result;
        var formattedContent = responseContent.Length > 0 ? $"{responseContent}, " : string.Empty;

        return $"{nameof(HttpFunctionException)}: " +
               $"[{(int)ex.HttpStatusCode}, {ex.HttpStatusCode}] " +
               $"{formattedContent}{ex.Message}";
    }
}