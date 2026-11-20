using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Newtonsoft.Json.Linq;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class FunctionAppHelper(
    ArmClient armClient, AzureResourceSettings resourceSettings, PollyRetry pollyRetry, RetrySettings retry)
{
    private const string RunningHostState = "Running";
    private static readonly HttpClient HostStatusClient = new();

    public async Task<string?> GetAppSetting(string functionAppName, string settingKey)
    {
        var settings = await GetSite(functionAppName).GetApplicationSettingsAsync();
        return settings.Value.Properties.TryGetValue(settingKey, out var value) ? value : null;
    }

    /// <summary>
    /// Sets the app settings in one update, so the function app restarts once; a null value removes its setting.
    /// </summary>
    public async Task SetAppSettings(string functionAppName, IReadOnlyDictionary<string, string?> values)
    {
        var site = GetSite(functionAppName);
        var settings = (await site.GetApplicationSettingsAsync()).Value;

        foreach (var (settingKey, value) in values)
        {
            if (value == null)
                settings.Properties.Remove(settingKey);
            else
                settings.Properties[settingKey] = value;
        }

        await site.UpdateApplicationSettingsAsync(settings);
    }

    /// <summary>
    /// Azure restarts the function app to apply a setting, and the app reads its settings only on start.
    /// So we wait for a host that started after the change; until then an old host may still process messages.
    /// </summary>
    /// <returns>True once a host started after the change is running; false if none did in time.</returns>
    public async Task<bool> SetAppSettingsAndWaitForRestart(string functionAppName, IReadOnlyDictionary<string, string?> values)
    {
        var description = string.Join(", ", values.Select(v => $"'{v.Key}' = '{v.Value ?? "(removed)"}'"));
        var changedAt = DateTimeOffset.MinValue;
        await pollyRetry.ExecuteWithRetry(async () =>
        {
            // Taken in the attempt that succeeds: Azure may have applied an attempt that timed out on our side,
            // and the host it restarted would otherwise pass for the restart of this one
            var attemptStartedAt = DateTimeOffset.UtcNow;
            await SetAppSettings(functionAppName, values);
            changedAt = attemptStartedAt;
        }, retry.ApiCall, $"Set {functionAppName} settings {description}");

        return await pollyRetry.WaitUntil(
            async () => await GetRunningHostStartTime(functionAppName) > changedAt,
            retry.FunctionAppRestart, $"Wait for {functionAppName} to restart after {changedAt:O}");
    }

    /// <summary>
    /// For settings that are already set: an earlier change may still be restarting the function app.
    /// </summary>
    /// <returns>True once a host is running; false if none is in time.</returns>
    public async Task<bool> WaitForRunningHost(string functionAppName)
    {
        return await pollyRetry.WaitUntil(
            async () => await GetRunningHostStartTime(functionAppName) != null,
            retry.FunctionAppRestart, $"Wait for {functionAppName} host to run");
    }

    /// <returns>App state as Azure reports it, e.g. "Running" or "Stopped".</returns>
    public async Task<string> GetState(string functionAppName)
    {
        return (await GetSite(functionAppName).GetAsync()).Value.Data.State;
    }

    /// <summary>
    /// Asks the Functions host for its status (admin endpoint, master key from ARM).
    /// </summary>
    /// <returns>When the running host process started, or null while the host is not running.</returns>
    public async Task<DateTimeOffset?> GetRunningHostStartTime(string functionAppName)
    {
        var site = GetSite(functionAppName);
        var hostName = (await site.GetAsync()).Value.Data.DefaultHostName;
        var masterKey = (await site.GetHostKeysAsync()).Value.MasterKey;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{hostName}/admin/host/status");
        request.Headers.Add("x-functions-key", masterKey);
        var requestedAt = DateTimeOffset.UtcNow;
        using var response = await HostStatusClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var status = JObject.Parse(await response.Content.ReadAsStringAsync());
        if (status.Value<string>("state") != RunningHostState)
            return null;

        return requestedAt - TimeSpan.FromMilliseconds(status.Value<long>("processUptime"));
    }

    private WebSiteResource GetSite(string functionAppName)
    {
        var resourceId = WebSiteResource.CreateResourceIdentifier(
            resourceSettings.SubscriptionId, resourceSettings.ResourceGroupName, functionAppName);
        return armClient.GetWebSiteResource(resourceId);
    }
}
