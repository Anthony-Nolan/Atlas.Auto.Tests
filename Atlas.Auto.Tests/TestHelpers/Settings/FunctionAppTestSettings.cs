namespace Atlas.Auto.Tests.TestHelpers.Settings;

/// <summary>
/// App settings some tests need on the Atlas function apps, marked with RequiresAppSetting. A [OneTimeSetUp] sets them before
/// the tests of their area (DonorImport: AssemblySetup) and restores the deployed values
/// after them, one restart per function app. They change nothing the other tests rely on, so every test still runs in parallel.
/// Azure:ChangeFunctionAppSettings false: nothing is changed and those tests are skipped.
/// </summary>
internal static class FunctionAppTestSettings
{
    /// <summary>QA-2908: donor import notifies about deleting a donor Atlas never had. Off by default on some environments.</summary>
    public const string NotifyOnUntrackedDonorDeletion = "NotificationConfiguration:NotifyOnAttemptedDeletionOfUntrackedDonor";

    /// <summary>Function app key (as in Azure:FunctionApps) → setting → value for the tests.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string?>> Values =
        new Dictionary<string, IReadOnlyDictionary<string, string?>>
        {
            ["DonorImport"] = new Dictionary<string, string?>
            {
                [NotifyOnUntrackedDonorDeletion] = "true"
            }
        };

    /// <returns>The function app key of a setting in <see cref="Values"/>.</returns>
    public static string AppKeyOf(string settingKey)
    {
        return Values.Single(app => app.Value.ContainsKey(settingKey)).Key;
    }
}
