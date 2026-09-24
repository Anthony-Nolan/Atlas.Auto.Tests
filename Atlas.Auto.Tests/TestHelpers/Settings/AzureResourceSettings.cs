namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class AzureResourceSettings
{
    public string SubscriptionId { get; set; } = "override-this";
    public string ResourceGroupName { get; set; } = "override-this";
    public Dictionary<string, string> FunctionApps { get; set; } = new();
}
