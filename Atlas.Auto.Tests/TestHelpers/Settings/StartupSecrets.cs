using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Atlas.Auto.Tests.TestHelpers.Settings;

/// <summary>
/// Reads the tests' secrets once, at start-up, before the services are built, so no copy of them lives in pipeline
/// variables, appsettings.json or the build artifact:
/// - the connection strings from the Atlas Key Vault of the environment under test (KeyVault:Name), where the Atlas
///   deployment keeps them;
/// - the Public API key: the default host key of the Public API function app (Azure:FunctionApps:PublicApi), through ARM.
/// A setting already set, e.g. in user secrets, is not read, so a local value still wins.
/// </summary>
/// <remarks>
/// Every secret that can't be read is reported in one exception, so one run shows all that is wrong. A failed sign-in
/// is reported once: no secret can be read then. No retries beyond the Azure SDK's own (transient errors): a 403 or 404
/// fails at once.
/// </remarks>
internal static class StartupSecrets
{
    /// <summary>Setting -> name of the secret in the Atlas Key Vault.</summary>
    private static readonly IReadOnlyDictionary<string, string> KeyVaultSecretNamesBySetting = new Dictionary<string, string>
    {
        ["BlobStorage:ConnectionString"] = "azure-storage-connection-string",
        // Listen only: the tests peek, so they never take a message away from Atlas or other subscribers
        ["ServiceBus:ConnectionString"] = "servicebus-read-only-connection-string",
        // The shared Atlas database: donors, search tracking, repeat search and match prediction
        ["Database:Sql"] = "match-prediction-sql-connection-string",
        ["Database:SqlA"] = "matching-transient-a-sql-connection-string",
        ["Database:SqlB"] = "matching-transient-b-sql-connection-string",
    };

    private const string PublicApiKeySetting = "PublicApi:ApiKey";
    private const string DefaultHostKeyName = "default";

    /// <returns>The secrets of the settings not set already.</returns>
    public static IReadOnlyDictionary<string, string?> Read(IConfiguration configuration, TokenCredential credential, ArmClient armClient)
    {
        var secrets = new Dictionary<string, string?>();
        var failures = new List<string>();

        try
        {
            ReadFromKeyVault(configuration, credential, secrets, failures);
            ReadPublicApiKey(configuration, armClient, secrets, failures);
        }
        catch (AuthenticationFailedException e)
        {
            throw new InvalidOperationException(
                $"Could not sign in to Azure as the identity running the tests, so no secret could be read: {e.Message}", e);
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Could not read the tests' secrets:\n" + string.Join("\n", failures.Select(f => $"  - {f}")));
        }

        return secrets;
    }

    private static void ReadFromKeyVault(
        IConfiguration configuration, TokenCredential credential, Dictionary<string, string?> secrets, List<string> failures)
    {
        var toRead = KeyVaultSecretNamesBySetting.Where(s => IsUnset(configuration, s.Key)).ToList();
        if (toRead.Count == 0)
            return;

        var vaultName = configuration["KeyVault:Name"];
        var client = new SecretClient(new Uri($"https://{vaultName}.vault.azure.net/"), credential);

        foreach (var (setting, secretName) in toRead)
        {
            try
            {
                secrets[setting] = client.GetSecret(secretName).Value.Value;
            }
            catch (Exception e) when (IsUnreachable(e))
            {
                // The same for every secret: report the vault once
                failures.Add($"{string.Join(", ", toRead.Select(s => s.Key))}: Key Vault '{vaultName}' can't be reached " +
                             $"({InnermostMessage(e)}). Check KeyVault:Name.");
                return;
            }
            catch (RequestFailedException e)
            {
                failures.Add($"{setting}: secret '{secretName}' in Key Vault '{vaultName}': {Describe(e)}. {KeyVaultHint(e.Status)}");
            }
        }
    }

    private static void ReadPublicApiKey(
        IConfiguration configuration, ArmClient armClient, Dictionary<string, string?> secrets, List<string> failures)
    {
        if (!IsUnset(configuration, PublicApiKeySetting))
            return;

        var appName = configuration["Azure:FunctionApps:PublicApi"]!;
        var source = $"the default host key of function app '{appName}'";
        try
        {
            var siteId = WebSiteResource.CreateResourceIdentifier(
                configuration["Azure:SubscriptionId"]!, configuration["Azure:ResourceGroupName"]!, appName);
            var keys = armClient.GetWebSiteResource(siteId).GetHostKeys().Value.FunctionKeys;

            if (keys.TryGetValue(DefaultHostKeyName, out var key))
                secrets[PublicApiKeySetting] = key;
            else
                failures.Add($"{PublicApiKeySetting}: function app '{appName}' has no default host key.");
        }
        catch (Exception e) when (IsUnreachable(e))
        {
            failures.Add($"{PublicApiKeySetting}: {source}: Azure Resource Manager can't be reached ({InnermostMessage(e)}).");
        }
        catch (RequestFailedException e)
        {
            failures.Add($"{PublicApiKeySetting}: {source}: {Describe(e)}. {ArmHint(e.Status)}");
        }
    }

    private static bool IsUnset(IConfiguration configuration, string setting) => string.IsNullOrEmpty(configuration[setting]);

    /// <summary>
    /// No response: e.g. a vault name that doesn't resolve. The Azure SDK throws an AggregateException when every retry failed.
    /// </summary>
    private static bool IsUnreachable(Exception e) => e switch
    {
        RequestFailedException { Status: 0 } => true,
        AggregateException aggregate => aggregate.InnerExceptions.All(IsUnreachable),
        _ => false
    };

    private static string InnermostMessage(Exception e)
    {
        while (e is AggregateException { InnerExceptions.Count: > 0 } aggregate)
            e = aggregate.InnerExceptions[^1];
        while (e.InnerException != null)
            e = e.InnerException;
        return e.Message.Split('\n')[0].Trim();
    }

    private static string Describe(RequestFailedException e) =>
        string.IsNullOrEmpty(e.ErrorCode) ? $"HTTP {e.Status}" : $"HTTP {e.Status} {e.ErrorCode}";

    private static string KeyVaultHint(int status) => status switch
    {
        401 => "The token is for another tenant: set Azure:TenantId to the vault's tenant.",
        403 => "The identity running the tests needs Key Vault Secrets User on the vault, or the vault's firewall blocks this client.",
        404 => "The vault has no secret of that name.",
        _ => ""
    };

    private static string ArmHint(int status) => status switch
    {
        401 => "The token is for another tenant: set Azure:TenantId to the subscription's tenant.",
        403 => "The identity running the tests needs to list the app's keys (Microsoft.Web/sites/host/listkeys/action), e.g. as Website Contributor.",
        404 => "No such function app: check Azure:SubscriptionId, Azure:ResourceGroupName and Azure:FunctionApps:PublicApi.",
        _ => ""
    };
}
