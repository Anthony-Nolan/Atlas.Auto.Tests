using System.Reflection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Services.DonorImport;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Debug.Client;
using Atlas.Debug.Client.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Atlas.Auto.Tests.DependencyInjection.Utils;

namespace Atlas.Auto.Tests.DependencyInjection;

internal static class ServiceConfiguration
{
    internal static IServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();

        services.SetUpConfiguration();

        services.RegisterSettings();

        services.RegisterDebugClients(
            OptionsReaderFor<DonorImportHttpFunctionSettings>(),
            OptionsReaderFor<MatchingAlgorithmHttpFunctionSettings>(),
            OptionsReaderFor<TopLevelHttpFunctionSettings>());

        services.RegisterTestServices();

        return services.BuildServiceProvider();
    }

    private static void SetUpConfiguration(this IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddUserSecrets(Assembly.GetExecutingAssembly())
            .Build();

        services.AddSingleton<IConfiguration>(sp => configuration);
    }

    private static void RegisterSettings(this IServiceCollection services)
    {
        services.RegisterAsOptions<DonorImportHttpFunctionSettings>("DonorImport");
        services.RegisterAsOptions<MatchingAlgorithmHttpFunctionSettings>("MatchingAlgorithm");
        services.RegisterAsOptions<TopLevelHttpFunctionSettings>("TopLevel");
    }

    private static void RegisterTestServices(this IServiceCollection services)
    {
        services.AddTransient<IDebugRequester, DebugRequester>();
        services.AddTransient<IMessageFetcher, MessageFetcher>();

        services.AddTransient<IDonorImportWorkflow, DonorImportWorkflow>();
        services.AddTransient<IFileImporter, FileImporter>();
        services.AddTransient<IImportResultFetcher, ImportResultFetcher>();
        services.AddTransient<IDonorStoreChecker, DonorStoreChecker>();
        services.AddTransient<IActiveMatchingDbChecker, ActiveMatchingDbChecker>();
        services.AddTransient<IFullModeChecker, FullModeChecker>();
        services.AddTransient<IFailedFileAlertFetcher, FailedFileAlertFetcher>();
        services.AddTransient<IHlaExpansionFailureAlertFetcher, HlaExpansionFailureAlertFetcher>();
    }
}