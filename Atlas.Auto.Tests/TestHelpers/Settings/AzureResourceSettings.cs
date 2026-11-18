namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class AzureResourceSettings
{
    public string SubscriptionId { get; set; } = "override-this";
    public string ResourceGroupName { get; set; } = "override-this";

    /// <summary>
    /// Whether the tests may change app settings of the function apps (<see cref="FunctionAppTestSettings"/>), which restarts them.
    /// False: nothing is changed, and the tests marked with RequiresAppSetting are skipped.
    /// </summary>
    public bool ChangeFunctionAppSettings { get; set; } = true;
    public Dictionary<string, string> FunctionApps { get; set; } = new();
}
