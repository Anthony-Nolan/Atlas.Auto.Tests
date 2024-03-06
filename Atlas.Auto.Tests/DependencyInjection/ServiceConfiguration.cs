using System.Reflection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Services.DonorDeletion;
using Atlas.Auto.Tests.TestHelpers.Services.DonorImport;
using Atlas.Auto.Tests.TestHelpers.Services.RepeatSearch;
using Atlas.Auto.Tests.TestHelpers.Services.Scoring;
using Atlas.Auto.Tests.TestHelpers.Services.Search;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
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
            OptionsReaderFor<TopLevelHttpFunctionSettings>(),
            OptionsReaderFor<PublicApiHttpFunctionSettings>(),
            OptionsReaderFor<RepeatSearchHttpFunctionSettings>());

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
        services.RegisterAsOptions<PublicApiHttpFunctionSettings>("PublicApi");
        services.RegisterAsOptions<RepeatSearchHttpFunctionSettings>("RepeatSearch");
    }

    private static void RegisterTestServices(this IServiceCollection services)
    {
        services.AddTransient<ITestDonorDeleter, TestDonorDeleter>();
        services.AddTransient<IDonorCodeFetcher, DonorCodeFetcher>();
        services.AddTransient<IDonorDeleter, DonorDeleter>();
        services.AddTransient<IAvailabilitySetter, AvailabilitySetter>();

        services.AddTransient<IDebugRequester, DebugRequester>();
        services.AddTransient<IMessageFetcher, MessageFetcher>();

        services.AddTransient(typeof(IHealthChecker<>), typeof(HealthChecker<>));

        // donor import
        services.AddTransient<IDonorImportTestSteps, DonorImportTestSteps>();
        services.AddTransient<IDonorImportWorkflow, DonorImportWorkflow>();
        services.AddTransient<IFileImporter, FileImporter>();
        services.AddTransient<IImportResultFetcher, ImportResultFetcher>();
        services.AddTransient<IDonorStoreChecker, DonorStoreChecker>();
        services.AddTransient<IActiveMatchingDbChecker, ActiveMatchingDbChecker>();
        services.AddTransient<IFullModeChecker, FullModeChecker>();
        services.AddTransient<IFailedFileAlertFetcher, FailedFileAlertFetcher>();
        services.AddTransient<IHlaExpansionFailureAlertFetcher, HlaExpansionFailureAlertFetcher>();
        services.AddTransient<IDonorImportFailureInfoFetcher, DonorImportFailureInfoFetcher>();

        // search
        services.AddTransient<ISearchTestSteps, SearchTestSteps>();
        services.AddTransient<ISearchWorkflow, SearchWorkflow>();
        services.AddTransient<ISearchRequester, SearchRequester>();
        services.AddTransient<IMatchingNotificationFetcher, MatchingNotificationFetcher>();
        services.AddTransient<IMatchingResultSetFetcher, MatchingResultSetFetcher>();
        services.AddTransient<ISearchNotificationFetcher, SearchNotificationFetcher>();
        services.AddTransient<ISearchResultSetFetcher, SearchResultSetFetcher>();

        // scoring
        services.AddTransient<IScoringTestSteps, ScoringTestSteps>();
        services.AddTransient<IScoringWorkflow, ScoringWorkflow>();
        services.AddTransient<IDonorScorer, DonorScorer>();

        // repeat search
        services.AddTransient<IRepeatSearchTestSteps, RepeatSearchTestSteps>();
        services.AddTransient<IRepeatSearchWorkflow, RepeatSearchWorkflow>();
        services.AddTransient<IRepeatSearchRequester, RepeatSearchRequester>();
        services.AddTransient<IRepeatMatchingNotificationFetcher, RepeatMatchingNotificationFetcher>();
        services.AddTransient<IRepeatMatchingResultSetFetcher, RepeatMatchingResultSetFetcher>();
        services.AddTransient<IRepeatSearchNotificationFetcher, RepeatSearchNotificationFetcher>();
        services.AddTransient<IRepeatSearchResultSetFetcher, RepeatSearchResultSetFetcher>();
    }
}