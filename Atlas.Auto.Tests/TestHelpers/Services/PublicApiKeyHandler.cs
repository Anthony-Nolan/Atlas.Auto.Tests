using Atlas.Auto.Tests.TestHelpers.Settings;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// The function key of the Public API: PublicApi:ApiKey when set, otherwise the app's default host key, read through ARM
/// once per test run, so no copy of the key is kept in settings.
/// </summary>
internal class PublicApiKeyProvider(
    PublicApiSettings settings, AzureResourceSettings azureResource, FunctionAppHelper functionAppHelper,
    PollyRetry pollyRetry, RetrySettings retry)
{
    private const string PublicApiFunctionAppKey = "PublicApi";

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _key = settings.ApiKey;

    public async Task<string> GetKey()
    {
        if (_key != null)
            return _key;

        await _lock.WaitAsync();
        try
        {
            if (_key == null)
            {
                var appName = azureResource.FunctionApps[PublicApiFunctionAppKey];
                _key = await pollyRetry.ExecuteWithRetry(
                    () => functionAppHelper.GetDefaultHostKey(appName), retry.ApiCall, $"Read the default host key of {appName}")
                    ?? throw new InvalidOperationException($"Function app {appName} has no default host key");
            }

            return _key;
        }
        finally
        {
            _lock.Release();
        }
    }
}

/// <summary>
/// Adds the Public API's function key to each request.
/// </summary>
internal class PublicApiKeyHandler(PublicApiKeyProvider keyProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("x-functions-key", await keyProvider.GetKey());
        return await base.SendAsync(request, cancellationToken);
    }
}
