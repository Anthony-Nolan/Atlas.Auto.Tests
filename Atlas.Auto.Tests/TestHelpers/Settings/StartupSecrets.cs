using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

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
/// Every secret that can't be read is reported in one exception, with the first line of the Azure error, which says what
/// is wrong. A failed sign-in is not collected: it is the same for every secret, so it is thrown as it is.
/// </remarks>
internal static class StartupSecrets
{
    /// <summary>
    /// Setting -> name of the secret in the Atlas Key Vault. A setting added here also needs its line in
    /// <see cref="ConfigurationValidator.ValidateSet"/> in ServiceConfiguration.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> KeyVaultSecretNamesBySetting = new Dictionary<string, string>
    {
        ["BlobStorage:ConnectionString"] = "azure-storage-connection-string",
        // Listen. The tests must only peek, so they never take a message away from Atlas or other subscribers: Listen
        // would also let them receive and complete.
        ["ServiceBus:ConnectionString"] = "servicebus-read-only-connection-string",
        // The shared Atlas database: donors, search tracking, repeat search and match prediction
        ["Database:Sql"] = "match-prediction-sql-connection-string",
        ["Database:SqlA"] = "matching-transient-a-sql-connection-string",
        ["Database:SqlB"] = "matching-transient-b-sql-connection-string",
    };

    private const string PublicApiKeySetting = "PublicApi:ApiKey";
    private const string DefaultHostKeyName = "default";

    /// <param name="hostKeyRetry">
    /// How long to wait for the Public API host to list its keys: right after a deployment it is still starting.
    /// </param>
    /// <returns>The secrets of the settings not set already.</returns>
    public static IReadOnlyDictionary<string, string?> Read(
        IConfiguration configuration, TokenCredential credential, ArmClient armClient, RetryPolicy hostKeyRetry)
    {
        var secrets = new Dictionary<string, string?>();
        var failures = new List<string>();

        void TryRead(string setting, string source, Func<string> read)
        {
            if (!string.IsNullOrEmpty(configuration[setting]))
                return;
            try
            {
                secrets[setting] = read();
            }
            catch (Exception e) when (e is not AuthenticationFailedException)
            {
                // After the Azure SDK's retries, e.g. for a vault name that doesn't resolve: the last attempt's error
                var error = e is AggregateException { InnerExceptions.Count: > 0 } retries ? retries.InnerExceptions[^1] : e;
                failures.Add($"{setting}: {source}: {FirstLine(error.Message)}");
            }
        }

        var vaultName = configuration["KeyVault:Name"];
        var secretClient = new SecretClient(new Uri($"https://{vaultName}.vault.azure.net/"), credential);
        foreach (var (setting, secretName) in KeyVaultSecretNamesBySetting)
        {
            TryRead(setting, $"secret '{secretName}' in Key Vault '{vaultName}'",
                () => secretClient.GetSecret(secretName).Value.Value);
        }

        var appName = configuration["Azure:FunctionApps:PublicApi"]!;
        TryRead(PublicApiKeySetting, $"the default host key of function app '{appName}'", () =>
        {
            var siteId = WebSiteResource.CreateResourceIdentifier(
                configuration["Azure:SubscriptionId"]!, configuration["Azure:ResourceGroupName"]!, appName);
            return ReadDefaultHostKey(armClient.GetWebSiteResource(siteId), appName, hostKeyRetry);
        });

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Could not read the tests' secrets:\n" + string.Join("\n", failures.Select(f => $"  - {f}")));
        }

        return secrets;
    }

    /// <summary>
    /// While its host is starting, e.g. right after a deployment, a function app fails to list its keys, often with a
    /// 400 ("error ... from host runtime"), which the Azure SDK doesn't retry. A wrong name or a missing role fails at once.
    /// </summary>
    private static string ReadDefaultHostKey(WebSiteResource site, string appName, RetryPolicy retry)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return site.GetHostKeys().Value.FunctionKeys[DefaultHostKeyName];
            }
            catch (RequestFailedException e) when (attempt <= retry.RetryCount && e.Status is not (0 or 401 or 403 or 404))
            {
                TestContext.Progress.WriteLine(
                    $"Function app '{appName}' can't list its keys yet ({FirstLine(e.Message)}); " +
                    $"retry {attempt} of {retry.RetryCount} in {retry.IntervalSeconds} s");
                Thread.Sleep(TimeSpan.FromSeconds(retry.IntervalSeconds));
            }
        }
    }

    private static string FirstLine(string message) => message.Split('\n')[0].Trim();
}
