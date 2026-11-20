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
        var settings = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        ConfigurationValidator.Validate(settings);

        var credential = AzureCredentialFactory.Create(settings["Azure:TenantId"]);
        var armClient = new ArmClient(credential);
        var retrySettings = settings.GetSection("Retry").Get<RetrySettings>() ?? new RetrySettings();

        var configuration = new ConfigurationBuilder()
            .AddConfiguration(settings)
            .AddInMemoryCollection(StartupSecrets.Read(settings, credential, armClient, retrySettings.FunctionAppRestart))
            .Build();

        var dbConfig = configuration.GetSection("Database").Get<DatabaseConfig>() ?? new DatabaseConfig();
        var publicApiSettings = configuration.GetSection("PublicApi").Get<PublicApiSettings>() ?? new PublicApiSettings();
        var sbSettings = configuration.GetSection("ServiceBus").Get<ServiceBusSettings>() ?? new ServiceBusSettings();
        var blobSettings = configuration.GetSection("BlobStorage").Get<BlobStorageSettings>() ?? new BlobStorageSettings();

        // Every setting StartupSecrets reads
        ConfigurationValidator.ValidateSet(
            ("Database:Sql", dbConfig.Sql),
            ("Database:SqlA", dbConfig.SqlA),
            ("Database:SqlB", dbConfig.SqlB),
            ("ServiceBus:ConnectionString", sbSettings.ConnectionString),
            ("BlobStorage:ConnectionString", blobSettings.ConnectionString),
            ("PublicApi:ApiKey", publicApiSettings.ApiKey));

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.Sink(new NUnitSink())
                .CreateLogger();
            builder.AddSerilog(serilogLogger, dispose: true);
            builder.AddFilter("Microsoft.EntityFrameworkCore",
                dbConfig.DebugPrint ? LogLevel.Information : LogLevel.Error);
        });

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
        services.AddSingleton<SearchTrackingSqlHelper>();

        services.AddSingleton(sbSettings);
        services.AddSingleton(new ServiceBusClient(sbSettings.ConnectionString));
        services.AddSingleton<SearchTrackingEventReader>();

        services.AddSingleton(blobSettings);
        services.AddSingleton(new BlobServiceClient(blobSettings.ConnectionString));
        services.AddSingleton<BlobStorageHelper>();

        var azureResource = configuration.GetSection("Azure").Get<AzureResourceSettings>() ?? new AzureResourceSettings();
        services.AddSingleton(azureResource);
        services.AddSingleton(armClient);
        services.AddSingleton<FunctionAppHelper>();

        var appInsightsSettings = configuration.GetSection("AppInsights").Get<AppInsightsSettings>() ?? new AppInsightsSettings();
        services.AddSingleton(appInsightsSettings);
        services.AddSingleton(new LogsQueryClient(credential, new LogsQueryClientOptions
        {
            Retry = { MaxRetries = 3, Mode = Azure.Core.RetryMode.Exponential }
        }));
        services.AddSingleton<AppInsightsHelper>();
        services.AddSingleton<AppInsightsIngestionCheck>();

        services.AddSingleton(retrySettings);

        services.AddSingleton<PollyRetry>();

        return services.BuildServiceProvider();
    }
}
