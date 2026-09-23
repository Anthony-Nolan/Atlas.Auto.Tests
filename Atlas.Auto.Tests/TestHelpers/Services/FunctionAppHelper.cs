using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class FunctionAppHelper(ArmClient armClient, AzureResourceSettings resourceSettings)
{
    public async Task<string?> GetAppSetting(string functionAppName, string settingKey)
    {
        var resourceId = WebSiteResource.CreateResourceIdentifier(
            resourceSettings.SubscriptionId, resourceSettings.ResourceGroupName, functionAppName);
        var site = armClient.GetWebSiteResource(resourceId);
        var settings = await site.GetApplicationSettingsAsync();
        return settings.Value.Properties.TryGetValue(settingKey, out var value) ? value : null;
    }
}
