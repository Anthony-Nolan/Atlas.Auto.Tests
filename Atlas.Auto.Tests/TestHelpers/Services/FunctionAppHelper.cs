using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Newtonsoft.Json.Linq;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class FunctionAppHelper(ArmClient armClient, AzureResourceSettings resourceSettings)
{
    private const string RunningHostState = "Running";
    private static readonly HttpClient HostStatusClient = new();

    public async Task<string?> GetAppSetting(string functionAppName, string settingKey)
    {
        var settings = await GetSite(functionAppName).GetApplicationSettingsAsync();
        return settings.Value.Properties.TryGetValue(settingKey, out var value) ? value : null;
    }

    /// <summary>
    /// Sets the app setting, or removes it when <paramref name="value"/> is null.
    /// Azure restarts the function app to apply the change.
    /// </summary>
    public async Task SetAppSetting(string functionAppName, string settingKey, string? value)
    {
        var site = GetSite(functionAppName);
        var settings = (await site.GetApplicationSettingsAsync()).Value;

        if (value == null)
            settings.Properties.Remove(settingKey);
        else
            settings.Properties[settingKey] = value;

        await site.UpdateApplicationSettingsAsync(settings);
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
