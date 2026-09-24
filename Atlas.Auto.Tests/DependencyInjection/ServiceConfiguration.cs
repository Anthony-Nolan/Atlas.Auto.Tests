using System.Reflection;
using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Azure.Messaging.ServiceBus;
using Azure.Monitor.Query;
using Azure.ResourceManager;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        ConfigurationValidator.Validate(configuration);

        var services = new ServiceCollection();
        var dbConfig = configuration.GetSection("Database").Get<DatabaseConfig>() ?? new DatabaseConfig();

        services.AddLogging(builder =>
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.Sink(new NUnitSink())
                .CreateLogger();
            builder.AddSerilog(serilogLogger, dispose: true);
            builder.AddFilter("Microsoft.EntityFrameworkCore",
                dbConfig.DebugPrint ? LogLevel.Information : LogLevel.Error);
        });

        var publicApiSettings = configuration.GetSection("PublicApi").Get<PublicApiSettings>() ?? new PublicApiSettings();
        services.AddSingleton(publicApiSettings);
        services.AddHttpClient<PublicApiClient>(client =>
        {
            client.BaseAddress = new Uri(publicApiSettings.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("x-functions-key", publicApiSettings.ApiKey);
        });

        services.AddDbContextFactory<AtlasContext>(options =>
        {
            options.UseSqlServer(dbConfig.Sql, dbConfig.ConfigureSqlServer);
            if (dbConfig.DebugPrint) options.EnableSensitiveDataLogging();
        });

        services.AddSingleton<IDbContextFactory<AtlasMatchingContext>>(sp =>
            new AtlasMatchingContextFactory(
                sp.GetRequiredService<IDbContextFactory<AtlasContext>>(),
                dbConfig,
                sp.GetRequiredService<ILoggerFactory>()));

        services.AddSingleton<IDonorStoreSqlHelper, DonorStoreSqlHelper>();

        var sbSettings = configuration.GetSection("ServiceBus").Get<ServiceBusSettings>() ?? new ServiceBusSettings();
        services.AddSingleton(sbSettings);
        services.AddSingleton(new ServiceBusClient(sbSettings.ConnectionString));

        var blobSettings = configuration.GetSection("BlobStorage").Get<BlobStorageSettings>() ?? new BlobStorageSettings();
        services.AddSingleton(blobSettings);
        services.AddSingleton(new BlobServiceClient(blobSettings.ConnectionString));
        services.AddSingleton<BlobStorageHelper>();

        var azureAuth = configuration.GetSection("Azure:Authentication").Get<AzureAuthSettings>() ?? new AzureAuthSettings();
        services.AddSingleton(azureAuth);

        var credential = azureAuth.CreateCredential();

        var azureResource = configuration.GetSection("Azure").Get<AzureResourceSettings>() ?? new AzureResourceSettings();
        services.AddSingleton(azureResource);
        services.AddSingleton(new ArmClient(credential));
        services.AddSingleton<FunctionAppHelper>();

        var appInsightsSettings = configuration.GetSection("AppInsights").Get<AppInsightsSettings>() ?? new AppInsightsSettings();
        services.AddSingleton(appInsightsSettings);
        services.AddSingleton(new LogsQueryClient(credential, new LogsQueryClientOptions
        {
            Retry = { MaxRetries = 3, Mode = Azure.Core.RetryMode.Exponential }
        }));
        services.AddSingleton<AppInsightsHelper>();

        services.AddSingleton(configuration.GetSection("Retry").Get<RetrySettings>() ?? new RetrySettings());

        services.AddSingleton<PollyRetry>();

        return services.BuildServiceProvider();
    }
}
