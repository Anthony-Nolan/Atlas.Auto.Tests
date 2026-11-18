using Atlas.Auto.Tests.TestHelpers.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Services;

/// <summary>
/// Sets <see cref="FunctionAppTestSettings"/> on the function apps before the tests and restores the deployed values after them.
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

    /// <summary>
    /// One update per app, so each restarts once; the apps restart at the same time.
    /// </summary>
    public async Task SetTestValues()
    {
        await Task.WhenAll(FunctionAppTestSettings.Values.Select(app => SetTestValues(app.Key, app.Value)));
    }

    /// <summary>
    /// Waits for the restarts too, so the next run does not start while the function apps restart.
    /// </summary>
    public async Task Restore()
    {
        await Task.WhenAll(FunctionAppTestSettings.Values.Keys.Select(Restore));
    }

    private async Task SetTestValues(string appKey, IReadOnlyDictionary<string, string?> testValues)
    {
        var appName = _azureResource.FunctionApps[appKey];
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

    private async Task Restore(string appKey)
    {
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
            _logger.LogInformation("{AppName:l} settings are already set", appName);
            return;
        }

        var restarted = await _functionAppHelper.SetAppSettingsAndWaitForRestart(appName, newValues);
        if (!restarted)
            throw new InvalidOperationException($"{appName} did not restart after setting {string.Join(", ", newValues)}");
    }
}
