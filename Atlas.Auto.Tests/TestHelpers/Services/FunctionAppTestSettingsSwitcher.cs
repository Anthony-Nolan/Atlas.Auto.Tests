using Atlas.Auto.Tests.TestHelpers.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Sets the <see cref="FunctionAppTestSettings"/> of one function app before the tests that need them, and restores the deployed
/// values after them. Changing a setting restarts the function app, so call it from a [OneTimeSetUp] that no test of that app overlaps.
/// </summary>
/// <remarks>
/// The deployed values are kept on the function app itself, in the <see cref="OriginalSettingsMarker"/> app setting, not in memory:
/// if a run is stopped before its teardown, the next run still restores the deployed values, not the test ones it finds.
/// Terraform owns all app settings, so a deployment removes the marker together with the test values.
/// </remarks>
internal class FunctionAppTestSettingsSwitcher(IServiceProvider provider)
{
    private const string OriginalSettingsMarker = "AutoTests:OriginalSettings";

    private readonly FunctionAppHelper _functionAppHelper = provider.GetRequiredService<FunctionAppHelper>();
    private readonly AzureResourceSettings _azureResource = provider.GetRequiredService<AzureResourceSettings>();
    private readonly ILogger _logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<FunctionAppTestSettingsSwitcher>();

    /// <param name="appKey">Function app key, as in Azure:FunctionApps and <see cref="FunctionAppTestSettings.Values"/>.</param>
    public async Task SetTestValues(string appKey)
    {
        if (!ChangeAllowed(appKey))
            return;

        var appName = _azureResource.FunctionApps[appKey];
        var testValues = FunctionAppTestSettings.Values[appKey];
        var current = await ReadSettings(appName, [.. testValues.Keys, OriginalSettingsMarker]);

        // A marker left by a run that stopped before its teardown holds the deployed values; the current ones are the test values
        var marker = current[OriginalSettingsMarker];
        var deployed = marker == null
            ? testValues.Keys.ToDictionary(key => key, key => current[key])
            : JsonConvert.DeserializeObject<Dictionary<string, string?>>(marker)!;
        if (marker != null)
            _logger.LogWarning("{AppName:l}: an earlier run did not restore its settings; deployed values: {Deployed:l}",
                appName, string.Join(", ", deployed));

        var newValues = new Dictionary<string, string?>(testValues)
        {
            [OriginalSettingsMarker] = JsonConvert.SerializeObject(deployed)
        };
        await SetIfChanged(appName, current, newValues);
    }

    /// <summary>
    /// Waits for the restart too, so the next tests or run do not start while the function app restarts.
    /// </summary>
    public async Task Restore(string appKey)
    {
        if (!ChangeAllowed(appKey))
            return;

        var appName = _azureResource.FunctionApps[appKey];
        var marker = await _functionAppHelper.GetAppSetting(appName, OriginalSettingsMarker);
        if (marker == null)
        {
            // Setup did not change this app, or a deployment has replaced its settings since
            _logger.LogInformation("{AppName:l}: no test settings to restore", appName);
            return;
        }

        var deployed = JsonConvert.DeserializeObject<Dictionary<string, string?>>(marker)!;
        var newValues = new Dictionary<string, string?>(deployed) { [OriginalSettingsMarker] = null };
        var current = await ReadSettings(appName, newValues.Keys);
        await SetIfChanged(appName, current, newValues);
    }

    /// <summary>
    /// Azure:ChangeFunctionAppSettings: false leaves the function apps as they are; the tests that need the settings are skipped.
    /// </summary>
    private bool ChangeAllowed(string appKey)
    {
        if (_azureResource.ChangeFunctionAppSettings)
            return true;

        _logger.LogInformation("Azure:ChangeFunctionAppSettings is false: {AppKey:l} settings are left as they are", appKey);
        return false;
    }

    private async Task<IReadOnlyDictionary<string, string?>> ReadSettings(string appName, IEnumerable<string> settingKeys)
    {
        var values = new Dictionary<string, string?>();
        foreach (var settingKey in settingKeys)
        {
            values[settingKey] = await _functionAppHelper.GetAppSetting(appName, settingKey);
        }
        _logger.LogInformation("{AppName:l} settings: {Settings:l}", appName, string.Join(", ", values));
        return values;
    }

    private async Task SetIfChanged(
        string appName, IReadOnlyDictionary<string, string?> currentValues, IReadOnlyDictionary<string, string?> newValues)
    {
        if (newValues.All(v => currentValues[v.Key] == v.Value))
        {
            // A run stopped right after it changed them may have left the function app restarting
            _logger.LogInformation("{AppName:l} settings are already set", appName);
            if (!await _functionAppHelper.WaitForRunningHost(appName))
                throw new InvalidOperationException($"{appName} host is not running");
            return;
        }

        var restarted = await _functionAppHelper.SetAppSettingsAndWaitForRestart(appName, newValues);
        if (!restarted)
            throw new InvalidOperationException($"{appName} did not restart after setting {string.Join(", ", newValues)}");
    }
}
