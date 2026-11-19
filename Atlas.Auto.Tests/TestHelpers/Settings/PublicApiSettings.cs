namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class PublicApiSettings
{
    public string BaseUrl { get; set; } = "override-this";

    /// <summary>
    /// Optional: unset, the tests read the default host key of the Public API function app (Azure:FunctionApps:PublicApi).
    /// </summary>
    public string? ApiKey { get; set; }
}
