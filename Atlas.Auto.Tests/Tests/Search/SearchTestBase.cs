using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.Tests.Search;

internal abstract class SearchTestBase : TestBase
{
    protected SearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected static IEnumerable<TestCaseData> Cases()
    {
        yield return new TestCaseData(null).SetName("{m}" + " (default)");
        yield return new TestCaseData(true).SetName("{m}" + " (new)");
        yield return new TestCaseData(false).SetName("{m}" + " (old)");
    }

    protected static async Task ExecuteWithRetry(Func<Task> action)
    {
        await PollyRetry.ExecuteWithRetry(action, 2, 0, "Test retry");
    }

    protected SearchTestSteps GetSearchTestSteps(string testName)
    {
        var testLogger = BuildTestLogger(testName);
        var importSteps = ResolveDonorImportStepsForSearchTests(testLogger);

        var publicApiClient = Provider.ResolveServiceOrThrow<IPublicApiFunctionsClient>();
        var matchingClient = Provider.ResolveServiceOrThrow<IMatchingAlgorithmFunctionsClient>();
        var topLevelClient = Provider.ResolveServiceOrThrow<ITopLevelFunctionsClient>();

        return new SearchTestSteps(
            req => publicApiClient.PostSearchRequest(req),
            new NotificationFetcher<MatchingResultsNotification>(
                req => matchingClient.PeekMatchingResultNotifications(req), 45, 20, "Fetch matching notification"),
            req => matchingClient.FetchMatchingResultSet(req),
            new NotificationFetcher<SearchResultsNotification>(
                req => topLevelClient.PeekSearchResultNotifications(req), 10, 20, "Fetch search notification"),
            req => topLevelClient.FetchSearchResultSet(req),
            importSteps,
            testLogger,
            testName);
    }

    protected DonorImportStepsForSearchTests ResolveDonorImportStepsForSearchTests(ITestLogger testLogger)
    {
        var donorImportWorkflow = Provider.ResolveServiceOrThrow<IDonorImportWorkflow>();
        var donorImportTestSteps = new DonorImportTestSteps(donorImportWorkflow, testLogger);
        return new DonorImportStepsForSearchTests(donorImportTestSteps, testLogger);
    }
}
