using Azure;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Atlas.Auto.Tests.TestHelpers.Settings;

/// <summary>
/// Reads the tests' secrets from the Atlas Key Vault of the environment under test (KeyVault:Name), where the Atlas
/// deployment keeps them, so no copy of them lives in pipeline variables, appsettings.json or the build artifact.
/// </summary>
internal static class KeyVaultSecrets
{
    /// <summary>Setting -> name of the secret in the Atlas Key Vault.</summary>
    private static readonly IReadOnlyDictionary<string, string> SecretNamesBySetting = new Dictionary<string, string>
    {
        ["BlobStorage:ConnectionString"] = "azure-storage-connection-string",
        // Listen only: the tests peek, so they never take a message away from Atlas or other subscribers
        ["ServiceBus:ConnectionString"] = "servicebus-read-only-connection-string",
        // The shared Atlas database: donors, search tracking, repeat search and match prediction
        ["Database:Sql"] = "match-prediction-sql-connection-string",
        ["Database:SqlA"] = "matching-transient-a-sql-connection-string",
        ["Database:SqlB"] = "matching-transient-b-sql-connection-string",
    };

    /// <returns>
    /// The secrets of the settings not set already, e.g. in user secrets, so a local value still wins.
    /// </returns>
    public static IReadOnlyDictionary<string, string?> Read(IConfiguration configuration, TokenCredential credential)
    {
        var toRead = SecretNamesBySetting.Where(s => string.IsNullOrEmpty(configuration[s.Key])).ToList();
        if (toRead.Count == 0)
            return new Dictionary<string, string?>();

        var vaultName = configuration["KeyVault:Name"];
        var client = new SecretClient(new Uri($"https://{vaultName}.vault.azure.net/"), credential);

        return toRead.ToDictionary(s => s.Key, s => (string?)GetSecret(client, vaultName, s.Value));
    }

    private static string GetSecret(SecretClient client, string? vaultName, string secretName)
    {
        try
        {
            return client.GetSecret(secretName).Value.Value;
        }
        catch (RequestFailedException e)
        {
            throw new InvalidOperationException(
                $"Could not read secret '{secretName}' from Key Vault '{vaultName}' (HTTP {e.Status}, {e.ErrorCode}). " +
                "The identity running the tests needs Key Vault Secrets User on it.", e);
        }
    }
}
