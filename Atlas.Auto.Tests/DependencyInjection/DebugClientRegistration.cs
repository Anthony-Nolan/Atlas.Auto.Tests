using Atlas.Debug.Client.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.DependencyInjection;

// TODO: move implementation to Atlas.Debug.Client
internal static class DebugClientRegistration
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    internal static void RegisterDebugClients(
        this IServiceCollection services,
        IConfigurationRoot configuration,
        params (string Section, Type Interface, Type Client)[] clients)
    {
        foreach (var (section, iface, client) in clients)
        {
            var settings = GetSettingsOrThrow(configuration, section);

            services.AddHttpClient(section, httpClient =>
            {
                httpClient.BaseAddress = new Uri(settings.BaseUrl);
                httpClient.Timeout = settings.RequestTimeOut ?? DefaultTimeout;
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("x-functions-key", settings.ApiKey);
            });

            services.AddTransient(iface, sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return Activator.CreateInstance(client, factory.CreateClient(section))!;
            });
        }
    }

    private class DebugClientSettings : HttpFunctionSettings;

    private static HttpFunctionSettings GetSettingsOrThrow(IConfigurationRoot configuration, string sectionName)
    {
        var settings = configuration.GetSection(sectionName).Get<DebugClientSettings>();

        if (settings == null)
        {
            throw new InvalidOperationException(
                $"Configuration section '{sectionName}' is missing. " +
                $"Add it to appsettings.json or user secrets with 'BaseUrl' and 'ApiKey' values.");
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.BaseUrl)
            || string.Equals(settings.BaseUrl, "override-this", StringComparison.OrdinalIgnoreCase)
            || !Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _))
            errors.Add($"'{sectionName}:BaseUrl' is not configured or not a valid URL (current value: '{settings.BaseUrl ?? "null"}')");

        if (string.IsNullOrWhiteSpace(settings.ApiKey)
            || string.Equals(settings.ApiKey, "override-this", StringComparison.OrdinalIgnoreCase))
            errors.Add($"'{sectionName}:ApiKey' is not configured (current value: '{settings.ApiKey ?? "null"}')");

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Configuration errors in '{sectionName}':\n  - {string.Join("\n  - ", errors)}\n" +
                $"Set the correct values in user secrets (dotnet user-secrets set \"{sectionName}:BaseUrl\" \"<url>\").");
        }

        return settings;
    }
}
