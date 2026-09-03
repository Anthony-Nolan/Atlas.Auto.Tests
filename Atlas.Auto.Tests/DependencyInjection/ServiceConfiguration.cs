using System.Reflection;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Debug.Client;
using Atlas.Debug.Client.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Atlas.Auto.Tests.DependencyInjection;

internal static class ServiceConfiguration
{
    internal static IServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        var donorImport = GetSettingsOrThrow<DonorImportHttpFunctionSettings>(configuration, "DonorImport");
        var matching = GetSettingsOrThrow<MatchingAlgorithmHttpFunctionSettings>(configuration, "MatchingAlgorithm");
        var topLevel = GetSettingsOrThrow<TopLevelHttpFunctionSettings>(configuration, "TopLevel");
        var publicApi = GetSettingsOrThrow<PublicApiHttpFunctionSettings>(configuration, "PublicApi");
        var repeatSearch = GetSettingsOrThrow<RepeatSearchHttpFunctionSettings>(configuration, "RepeatSearch");

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.Sink(new NUnitSink())
                .CreateLogger();
            builder.AddSerilog(serilogLogger, dispose: true);
        });

        services.RegisterDebugClients(
            _ => donorImport,
            _ => matching,
            _ => topLevel,
            _ => publicApi,
            _ => repeatSearch);

        services.AddSingleton(configuration.GetSection("Retry").Get<RetrySettings>() ?? new RetrySettings());

        services.AddSingleton<PollyRetry>();

        services.AddTransient<TestDonorDeleter>();

        services.AddTransient<DonorImportWorkflow>();

        return services.BuildServiceProvider();
    }

    private static T GetSettingsOrThrow<T>(IConfigurationRoot configuration, string sectionName) where T : HttpFunctionSettings
    {
        var settings = configuration.GetSection(sectionName).Get<T>();

        var errors = new List<string>();

        if (settings == null)
        {
            throw new InvalidOperationException(
                $"Configuration section '{sectionName}' is missing. " +
                $"Add it to appsettings.json or user secrets with 'BaseUrl' and 'ApiKey' values.");
        }

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
