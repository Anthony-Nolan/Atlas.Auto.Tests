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
}
