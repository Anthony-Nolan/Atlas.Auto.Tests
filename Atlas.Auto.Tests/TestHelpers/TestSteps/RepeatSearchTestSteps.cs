using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.Debug.Client.Models.SearchResults;
using Atlas.Debug.Client.Models.Validation;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class RepeatSearchTestSteps : SearchTestStepsBase
{
    private readonly Func<RepeatSearchRequest, Task<ResponseFromValidatedRequest<SearchInitiationResponse>>> _postRepeatSearchRequest;
    private readonly NotificationFetcher<MatchingResultsNotification> _matchingNotificationFetcher;
    private readonly Func<DebugSearchResultsRequest, Task<RepeatMatchingAlgorithmResultSet>> _fetchMatchingResultSet;
    private readonly NotificationFetcher<SearchResultsNotification> _searchNotificationFetcher;
    private readonly Func<DebugSearchResultsRequest, Task<RepeatSearchResultSet>> _fetchSearchResultSet;
    private readonly SearchTestSteps _searchTestSteps;

    public RepeatSearchTestSteps(
        Func<RepeatSearchRequest, Task<ResponseFromValidatedRequest<SearchInitiationResponse>>> postRepeatSearchRequest,
        NotificationFetcher<MatchingResultsNotification> matchingNotificationFetcher,
        Func<DebugSearchResultsRequest, Task<RepeatMatchingAlgorithmResultSet>> fetchMatchingResultSet,
        NotificationFetcher<SearchResultsNotification> searchNotificationFetcher,
        Func<DebugSearchResultsRequest, Task<RepeatSearchResultSet>> fetchSearchResultSet,
        SearchTestSteps searchTestSteps,
        IDonorImportStepsForSearchTests donorImportSteps,
        ITestLogger logger,
        string testName)
        : base(donorImportSteps, logger, testName)
    {
        _postRepeatSearchRequest = postRepeatSearchRequest;
        _matchingNotificationFetcher = matchingNotificationFetcher;
        _fetchMatchingResultSet = fetchMatchingResultSet;
        _searchNotificationFetcher = searchNotificationFetcher;
        _fetchSearchResultSet = fetchSearchResultSet;
        _searchTestSteps = searchTestSteps;
    }

    public async Task<string> CreateMatchingDonor(ImportDonorType donorType)
    {
        return await donorImportSteps.CreateDonorWithSearchTestPhenotype(donorType);
    }

    public async Task<string> CreateNonMatchingDonor(ImportDonorType donorType)
    {
        return await donorImportSteps.CreateDonorWithValidDnaPhenotype(donorType);
    }

    public async Task EditDonorToNoLongerMatch(string donorCode, ImportDonorType donorType)
    {
        await donorImportSteps.EditDonorHlaToValidDnaPhenotype(donorCode, donorType);
    }

    public async Task EditDonorToMatch(string donorCode, ImportDonorType donorType)
    {
        await donorImportSteps.EditDonorHlaToSearchTestPhenotype(donorCode, donorType);
    }

    public async Task DeleteDonors(IReadOnlyCollection<string> donorCodes)
    {
        await donorImportSteps.DeleteDonors(donorCodes);
    }

    public async Task<string> OriginalSearchShouldOnlyReturnExpectedDonors(
        string searchRequestFileName, DonorChanges donorChanges, bool? parallelMatchPrediction = null)
    {
        var response = await _searchTestSteps.SubmitSearchRequest(searchRequestFileName, parallelMatchPrediction);
        await _searchTestSteps.MatchingShouldOnlyReturnExpectedDonors(response.SearchIdentifier, donorChanges);
        return response.SearchIdentifier;
    }

    public async Task<string> SubmitRepeatSearchRequest(
        string searchRequestFileName,
        string originalSearchId,
        DateTimeOffset searchCutOff,
        bool? parallelMatchPrediction = null)
    {
        var originalSearchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);
        originalSearchRequest.ParallelMatchPrediction = parallelMatchPrediction;

        var repeatRequest = new RepeatSearchRequest
        {
            OriginalSearchId = originalSearchId,
            SearchRequest = originalSearchRequest,
            SearchCutoffDate = searchCutOff
        };

        var response = await PollyRetry.ExecuteWithRetry(
            async () => await _postRepeatSearchRequest(repeatRequest), 5, 5, "Submit repeat search request");
        var result = AssertNotNull(response, "Repeat search API should have responded", "Submit repeat search request");

        logger.AssertThenLogAndThrow(
            () => result.WasSuccess.Should().BeTrue(
                "Repeat search request should have been accepted but got validation failures: {0}",
                string.Join(", ", result.ValidationFailures?.Select(f => f.ErrorMessage) ?? Array.Empty<string>())),
            "Validate repeat search response is successful");

        return result.ResponseOnSuccess!.RepeatSearchIdentifier;
    }

    public async Task RepeatMatchingShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId,
        string searchId,
        DonorChanges donorChanges)
    {
        const string action = "Check repeat matching identifies expected changes";
        logger.LogStart(action);

        var notification = await FetchMatchingResultsNotification(repeatSearchId, searchId);
        notification.MatchingShouldHaveBeenSuccessful();

        var matchingResultSet = await PollyRetry.ExecuteWithRetry(
            async () => await _fetchMatchingResultSet(notification.ToDebugSearchResultsRequest()), 5, 10, "Fetch repeat matching result set");
        AssertNotNull(matchingResultSet,
            "Matching result set should have been fetched for repeat search",
            "Fetch matching result set");

        ExpectedDonorsShouldNoLongerMatch(matchingResultSet!.NoLongerMatchingDonors, donorChanges.NoLongerMatching);

        foreach (var newlyMatchedDonor in donorChanges.NewlyMatching)
        {
            var donorResult = matchingResultSet.GetDonorResult(newlyMatchedDonor);
            await DonorResultShouldBeAsExpected(donorResult, "MatchingResult");
        }

        logger.LogCompletion(action);
    }

    public async Task RepeatSearchShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId,
        string searchId,
        DonorChanges donorChanges)
    {
        const string action = "Check repeat search identifies expected changes";
        logger.LogStart(action);

        var notification = await FetchSearchResultsNotification(repeatSearchId, searchId);
        notification.SearchShouldHaveBeenSuccessful();

        var searchResultSet = await PollyRetry.ExecuteWithRetry(
            async () => await _fetchSearchResultSet(notification.ToDebugSearchResultsRequest()), 5, 10, "Fetch repeat search result set");
        AssertNotNull(searchResultSet,
            "Search result set should have been fetched for repeat search",
            "Fetch search result set");

        ExpectedDonorsShouldNoLongerMatch(searchResultSet!.NoLongerMatchingDonorCodes, donorChanges.NoLongerMatching);

        foreach (var newlyMatchedDonor in donorChanges.NewlyMatching)
        {
            var donorResult = searchResultSet.GetDonorResult(newlyMatchedDonor);
            await DonorResultShouldBeAsExpected(donorResult, "SearchResult");
        }

        logger.LogCompletion(action);
    }

    public async Task RepeatRequestMissingRequiredInfoShouldReturnValidationErrors()
    {
        var response = await PollyRetry.ExecuteWithRetry(
            async () => await _postRepeatSearchRequest(new RepeatSearchRequest()), 5, 5, "Submit repeat search request");
        var result = AssertNotNull(response, "Repeat search API should have responded", "Submit invalid repeat search request");

        logger.AssertThenLogAndThrow(
            () => result.WasSuccess.Should().BeFalse(
                "Repeat search request should have been rejected with validation failures but was accepted"),
            "Validate repeat search response is a validation failure");

        var validationErrors = result.ValidationFailures!.ToList();
        validationErrors.ShouldContain("'Original Search Id' must not be empty.");
        validationErrors.ShouldContain("'Search Cutoff Date' must not be empty.");
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string repeatSearchId, string searchId)
    {
        var notification = await _matchingNotificationFetcher.FetchNotification(
            m => m.RepeatSearchRequestId == repeatSearchId && m.SearchRequestId == searchId);
        return AssertNotNull(notification,
            $"Matching notification should have been received for repeat search {repeatSearchId}",
            "Fetch matching results notification");
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string repeatSearchId, string searchId)
    {
        var notification = await _searchNotificationFetcher.FetchNotification(
            m => m.RepeatSearchRequestId == repeatSearchId && m.SearchRequestId == searchId);
        return AssertNotNull(notification,
            $"Search notification should have been received for repeat search {repeatSearchId}",
            "Fetch search results notification");
    }

    private void ExpectedDonorsShouldNoLongerMatch(
        IEnumerable<string> noLongerMatchingDonors,
        IEnumerable<string> expectedDonorCodes)
    {
        logger.AssertThenLogAndThrow(
            () => noLongerMatchingDonors.Should().Contain(expectedDonorCodes),
            "Check for no longer matching donors");
    }
}
