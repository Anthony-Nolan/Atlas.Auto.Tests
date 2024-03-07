using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results;
using Atlas.Client.Models.Search.Results.Matching.ResultSet;
using Atlas.Client.Models.Search.Results.ResultSet;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

/// <summary>
/// Steps for testing Atlas search.
/// Covers arrangement and execution of the repeat search workflow and assertion of its outcomes.
/// </summary>
internal interface IRepeatSearchTestSteps
{
    /// <returns>donor record id</returns>
    Task<string> CreateMatchingDonor(ImportDonorType donorType);

    /// <returns>donor record id</returns>
    Task<string> CreateNonMatchingDonor(ImportDonorType donorType);

    Task EditDonorToNoLongerMatch(string donorCode, ImportDonorType donorType);

    Task EditDonorToMatch(string donorCode, ImportDonorType donorType);

    Task DeleteDonors(IReadOnlyCollection<string> donorCodes);

    /// <returns>search request id</returns>
    Task<string> OriginalSearchShouldOnlyReturnExpectedDonors(
        string searchRequestFileName, DonorChanges donorChanges);

    /// <returns>repeat search request id</returns>
    Task<string> SubmitRepeatSearchRequest(
        string searchRequestFileName, string originalSearchId, DateTimeOffset searchCutOff);

    Task RepeatMatchingShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId, string searchId, DonorChanges donorChanges);

    Task RepeatSearchShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId, string searchId, DonorChanges donorChanges);
}

internal class RepeatSearchTestSteps : IRepeatSearchTestSteps
{
    private readonly IRepeatSearchWorkflow workflow;
    private readonly IDonorImportStepsForSearchTests donorImportSteps;
    private readonly ISearchTestSteps searchTestSteps;
    private readonly ITestLogger logger;
    private readonly string testName;

    public RepeatSearchTestSteps(
        IRepeatSearchWorkflow workflow,
        IDonorImportStepsForSearchTests donorImportSteps,
        ISearchTestSteps searchTestSteps,
        ITestLogger logger,
        string testName)
    {
        this.workflow = workflow;
        this.logger = logger;
        this.testName = testName;
        this.searchTestSteps = searchTestSteps;
        this.donorImportSteps = donorImportSteps;
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

    public async Task<string> OriginalSearchShouldOnlyReturnExpectedDonors(string searchRequestFileName, DonorChanges donorChanges)
    {
        var response = await searchTestSteps.SubmitSearchRequest(searchRequestFileName);
        await searchTestSteps.MatchingShouldOnlyReturnExpectedDonors(response.SearchIdentifier, donorChanges);
        // no need to assert the final search result as repeat search only uses the matching result as input
        return response.SearchIdentifier;
    }

    public async Task<string> SubmitRepeatSearchRequest(
        string searchRequestFileName,
        string originalSearchId,
        DateTimeOffset searchCutOff)
    {
        var originalSearchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);

        var repeatRequest = new RepeatSearchRequest
        {
            OriginalSearchId = originalSearchId,
            SearchRequest = originalSearchRequest,
            SearchCutoffDate = searchCutOff
        };

        var response = await workflow.SubmitRepeatSearchRequest(repeatRequest);
        logger.AssertResponseThenLogAndThrow(response, "Submit repeat search request");
        return response.DebugResult!.RepeatSearchIdentifier;
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

        var matchingResultSet = await FetchMatchingAlgorithmResultSet(notification);
        ExpectedDonorsShouldNoLongerMatch(matchingResultSet.NoLongerMatchingDonors, donorChanges.NoLongerMatching);

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

        var searchResultSet = await FetchSearchResultSet(notification);
        ExpectedDonorsShouldNoLongerMatch(searchResultSet.NoLongerMatchingDonorCodes, donorChanges.NoLongerMatching);

        foreach (var newlyMatchedDonor in donorChanges.NewlyMatching)
        {
            var donorResult = searchResultSet.GetDonorResult(newlyMatchedDonor);
            await DonorResultShouldBeAsExpected(donorResult, "SearchResult");
        }

        logger.LogCompletion(action);
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string repeatSearchId, string searchId)
    {
        var notificationResponse = await workflow.FetchMatchingResultsNotification(repeatSearchId, searchId);
        logger.AssertResponseThenLogAndThrow(notificationResponse, "Fetch matching results notification");
        return notificationResponse.DebugResult!;
    }

    private async Task<RepeatMatchingAlgorithmResultSet> FetchMatchingAlgorithmResultSet(ResultsNotification notification)
    {
        var resultSetResponse = await workflow.FetchMatchingResultSet(notification.ToDebugSearchResultsRequest());
        logger.AssertResponseThenLogAndThrow(resultSetResponse, "Fetch matching result set");
        return resultSetResponse.DebugResult!;
    }

    private async Task<SearchResultsNotification> FetchSearchResultsNotification(string repeatSearchId, string searchId)
    {
        var notificationResponse = await workflow.FetchSearchResultsNotification(repeatSearchId, searchId);
        logger.AssertResponseThenLogAndThrow(notificationResponse, "Fetch search results notification");
        return notificationResponse.DebugResult!;
    }

    private async Task<RepeatSearchResultSet> FetchSearchResultSet(ResultsNotification notification)
    {
        var resultSetResponse = await workflow.FetchSearchResultSet(notification.ToDebugSearchResultsRequest());
        logger.AssertResponseThenLogAndThrow(resultSetResponse, "Fetch search result set");
        return resultSetResponse.DebugResult!;
    }

    private void ExpectedDonorsShouldNoLongerMatch(
        IEnumerable<string> noLongerMatchingDonors,
        IEnumerable<string> expectedDonorCodes)
    {
        logger.AssertThenLogAndThrow(
            () => noLongerMatchingDonors.Should().Contain(expectedDonorCodes),
            "Check for no longer matching donors");
    }

    private async Task DonorResultShouldBeAsExpected<TResult>(TResult? donorResult, string approvalFileNameSuffix) where TResult : Result
    {
        logger.AssertThenLogAndThrow(() => donorResult.Should().NotBeNull(), "Select donor result");

        await logger.AssertThenLogAndThrowAsync(
            () => VerifyJson(donorResult.SerializeSingle())
                .IgnoreVaryingSearchResultProperties()
                .WriteReceivedToApprovalsFolder($"{testName}_{approvalFileNameSuffix}"),
            $"Comparison of donor {donorResult!.DonorCode} to approved result");
    }
}