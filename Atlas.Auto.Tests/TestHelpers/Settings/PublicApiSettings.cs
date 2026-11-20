namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class PublicApiSettings
{
    public string BaseUrl { get; set; } = "override-this";

    /// <summary>
    /// Read at start-up (<see cref="StartupSecrets"/>): the default host key of the Public API function app
    /// (Azure:FunctionApps:PublicApi), unless set, e.g. in user secrets.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
