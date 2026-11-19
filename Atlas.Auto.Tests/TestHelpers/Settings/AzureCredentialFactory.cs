using Azure.Core;
using Azure.Identity;

namespace Atlas.Auto.Tests.TestHelpers.Settings;

/// <summary>
/// The identity the tests use for Azure: ARM, App Insights and the Atlas Key Vault.
/// </summary>
internal static class AzureCredentialFactory
{
    // Set by azure-test-run-template.yml on the steps that run the tests, for the pipeline's workload identity federation
    // service connection. The names are the ones the Azure SDK documents for AzurePipelinesCredential.
    private const string TenantIdVariable = "AZURESUBSCRIPTION_TENANT_ID";
    private const string ClientIdVariable = "AZURESUBSCRIPTION_CLIENT_ID";
    private const string ServiceConnectionIdVariable = "AZURESUBSCRIPTION_SERVICE_CONNECTION_ID";
    private const string SystemAccessTokenVariable = "SYSTEM_ACCESSTOKEN";

    private static readonly string[] PipelineVariables =
        [TenantIdVariable, ClientIdVariable, ServiceConnectionIdVariable, SystemAccessTokenVariable];

    /// <param name="localTenantId">
    /// Azure:TenantId, optional: the tenant for a local `az login` whose default tenant is not the one Atlas is in.
    /// </param>
    public static TokenCredential Create(string? localTenantId)
    {
        var values = PipelineVariables.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        // An undefined pipeline variable reaches the step as its literal macro, e.g. "$(E2E.TenantId)"
        var missing = values
            .Where(v => string.IsNullOrEmpty(v.Value) || v.Value.StartsWith("$("))
            .Select(v => v.Key)
            .ToList();

        // Locally: whoever ran `az login`
        if (missing.Count == PipelineVariables.Length)
            return new AzureCliCredential(new AzureCliCredentialOptions { TenantId = localTenantId });

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "The pipeline sets only some of the variables for its Azure service connection. Missing:\n" +
                string.Join("\n", missing.Select(name => $"  - {name}")));
        }

        // Asks Azure DevOps for a new token whenever it needs one, so a run longer than one token's lifetime keeps working
        return new AzurePipelinesCredential(
            values[TenantIdVariable],
            values[ClientIdVariable],
            values[ServiceConnectionIdVariable],
            values[SystemAccessTokenVariable]);
    }
}
