using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class FunctionAppHelper(ArmClient armClient, AzureResourceSettings resourceSettings)
{
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

    private WebSiteResource GetSite(string functionAppName)
    {
        var resourceId = WebSiteResource.CreateResourceIdentifier(
            resourceSettings.SubscriptionId, resourceSettings.ResourceGroupName, functionAppName);
        return armClient.GetWebSiteResource(resourceId);
    }
}
