using System.Collections.Concurrent;
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
/// A set-up that fails does not fail the run: only the tests that need its settings fail (see <see cref="GetSetUpError"/>).
/// </remarks>
internal class FunctionAppTestSettingsSwitcher(IServiceProvider provider)
{
    private const string OriginalSettingsMarker = "AutoTests:OriginalSettings";

    // Function app key → why its test settings could not be set; read by RequiresAppSetting before each test that needs them
    private static readonly ConcurrentDictionary<string, string> SetUpErrors = new();

    private readonly FunctionAppHelper _functionAppHelper = provider.GetRequiredService<FunctionAppHelper>();
    private readonly AzureResourceSettings _azureResource = provider.GetRequiredService<AzureResourceSettings>();
    private readonly ILogger _logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<FunctionAppTestSettingsSwitcher>();

    /// <returns>Why the test settings of the function app could not be set, or null if they were.</returns>
    public static string? GetSetUpError(string appKey)
    {
        return SetUpErrors.GetValueOrDefault(appKey);
    }

    /// <summary>
    /// Does not throw: a failure is logged and kept for <see cref="GetSetUpError"/>, so the other tests of the run still run.
    /// </summary>
    /// <param name="appKey">Function app key, as in Azure:FunctionApps and <see cref="FunctionAppTestSettings.Values"/>.</param>
    public async Task SetTestValues(string appKey)
    {
        if (!ChangeAllowed(appKey))
            return;

        try
        {
            await ApplyTestValues(appKey);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{AppKey:l}: could not set the test settings; the tests that need them will fail", appKey);
            SetUpErrors[appKey] = e.Message;
        }
    }

    private async Task ApplyTestValues(string appKey)
    {
        var appName = _azureResource.FunctionApps[appKey];
        var testValues = FunctionAppTestSettings.Values[appKey];
        var current = await ReadSettings(appName, [.. testValues.Keys, OriginalSettingsMarker]);

        // A marker left by a run that stopped before its teardown holds the deployed values; the current ones are the test values.
        // A setting it does not hold was added since, so that run did not change it: its current value is the deployed one.
        var marker = current[OriginalSettingsMarker];
        var markerValues = marker == null ? [] : JsonConvert.DeserializeObject<Dictionary<string, string?>>(marker)!;
        var deployed = testValues.Keys.ToDictionary(
            key => key,
            key => markerValues.TryGetValue(key, out var deployedValue) ? deployedValue : current[key]);
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
