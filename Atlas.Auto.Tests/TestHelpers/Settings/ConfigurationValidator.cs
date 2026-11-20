using Microsoft.Extensions.Configuration;

namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal static class ConfigurationValidator
{
    private const string Placeholder = "override-this";

    internal static void Validate(IConfiguration configuration)
    {
        var unresolved = configuration
            .AsEnumerable()
            .Where(kv => kv.Value != null && kv.Value.Equals(Placeholder, StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToList();

        if (unresolved.Count > 0)
        {
            throw new InvalidOperationException(
                $"Configuration contains unresolved placeholder values. " +
                $"Set these in user secrets (secrets.json) or environment variables:\n" +
                string.Join("\n", unresolved.Select(k => $"  - {k}")));
        }
    }

    /// <summary>
    /// Fails on each of these settings, as the services get them, that is empty or still the placeholder: a setting
    /// that is not in appsettings.json, e.g. a secret StartupSecrets reads into the wrong key, binds to its default.
    /// </summary>
    internal static void ValidateSet(params (string Setting, string? Value)[] settings)
    {
        var unset = settings
            .Where(s => string.IsNullOrEmpty(s.Value) || s.Value.Equals(Placeholder, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Setting)
            .ToList();

        if (unset.Count > 0)
        {
            throw new InvalidOperationException(
                "These settings have no value after the secrets were read:\n" +
                string.Join("\n", unset.Select(k => $"  - {k}")));
        }
    }
}
