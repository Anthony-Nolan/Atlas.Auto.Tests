using System.Globalization;

namespace Atlas.Auto.Tests.TestHelpers.Settings;

/// <summary>
/// App settings some tests need on the Atlas function apps. AssemblySetup sets them all before the tests and restores
/// the original values after them, one restart per function app. They change nothing the other tests rely on,
/// so every test still runs in parallel.
/// </summary>
internal static class FunctionAppTestSettings
{
    /// <summary>QA-2908: donor import notifies about deleting a donor Atlas never had. Off by default on some environments.</summary>
    public const string NotifyOnUntrackedDonorDeletion = "NotificationConfiguration:NotifyOnAttemptedDeletionOfUntrackedDonor";

    /// <summary>
    /// QA-2921, ATL-527: a repeat search whose original result set is not stored yet is published again every this many seconds...
    /// </summary>
    public const string OriginalResultsWaitInterval = "RepeatSearch:OriginalResultsWaitIntervalSeconds";

    /// <summary>...until this many minutes after its first request; then it runs and fails validation.</summary>
    public const string MaxWaitForOriginalResults = "RepeatSearch:MaxWaitForOriginalResultsMinutes";

    /// <summary>Instead of 5 min on DEV.</summary>
    public static readonly TimeSpan ShortWaitInterval = TimeSpan.FromSeconds(20);

    /// <summary>Instead of 24 h on DEV, so QA-2921 sees the failure after the wait.</summary>
    public static readonly TimeSpan ShortMaxWait = TimeSpan.FromMinutes(1);

    /// <summary>Function app key (as in Azure:FunctionApps) → setting → value for the tests.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string?>> Values =
        new Dictionary<string, IReadOnlyDictionary<string, string?>>
        {
            ["DonorImport"] = new Dictionary<string, string?>
            {
                [NotifyOnUntrackedDonorDeletion] = "true"
            },
            ["RepeatSearch"] = new Dictionary<string, string?>
            {
                [OriginalResultsWaitInterval] = ShortWaitInterval.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                [MaxWaitForOriginalResults] = ShortMaxWait.TotalMinutes.ToString(CultureInfo.InvariantCulture)
            }
        };
}
