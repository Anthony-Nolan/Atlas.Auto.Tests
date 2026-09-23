using Azure.Core;
using Azure.Identity;

namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class AzureAuthSettings
{
    public string ClientId { get; set; } = "override-this";
    public string ClientSecret { get; set; } = "override-this";
    public string TenantId { get; set; } = "override-this";

    public TokenCredential CreateCredential()
        => new ClientSecretCredential(TenantId, ClientId, ClientSecret);
}
