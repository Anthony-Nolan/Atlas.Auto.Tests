using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.Auto.Tests.Tests.Search;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

internal abstract class RepeatSearchTestBase : SearchTestBase
{
    protected RepeatSearchTestBase(string testFixtureName) : base(testFixtureName)
    {
    }

    protected RepeatSearchTestSteps GetRepeatSearchTestSteps(string testName)
    {
        var searchTestSteps = GetSearchTestSteps(testName);

        var publicApiClient = Provider.ResolveServiceOrThrow<IPublicApiFunctionsClient>();
        var repeatSearchClient = Provider.ResolveServiceOrThrow<IRepeatSearchFunctionsClient>();
        var topLevelClient = Provider.ResolveServiceOrThrow<ITopLevelFunctionsClient>();

        return new RepeatSearchTestSteps(
            req => publicApiClient.PostRepeatSearchRequest(req),
            new NotificationFetcher<MatchingResultsNotification>(
                req => repeatSearchClient.PeekMatchingResultNotifications(req), 10, 20, "Fetch repeat matching notification"),
            req => repeatSearchClient.FetchMatchingResultSet(req),
            new NotificationFetcher<SearchResultsNotification>(
                req => topLevelClient.PeekRepeatSearchResultNotifications(req), 10, 20, "Fetch repeat search notification"),
            req => topLevelClient.FetchRepeatSearchResultSet(req),
            searchTestSteps,
            ResolveDonorImportStepsForSearchTests(searchTestSteps.Logger),
            searchTestSteps.Logger,
            testName);
    }
}
