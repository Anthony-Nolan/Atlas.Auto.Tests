using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Client.Models.Search.Requests;
using Atlas.Client.Models.Search.Results.Matching;
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
    Task<string> CreateDonor(ImportDonorType donorType);

    Task EditDonorToNoLongerMatch(string donorCode, ImportDonorType donorType);

    Task DeleteDonor(string donorCode);

    /// <returns>search request id</returns>
    Task<string> OriginalSearchShouldReturnExpectedDonor(string searchRequestFileName, string expectedDonorCode);

    /// <returns>repeat search request id</returns>
    Task<string> SubmitRepeatSearchRequest(
        string searchRequestFileName, string originalSearchId, DateTimeOffset searchCutOff);

    Task RepeatMatchingShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId, string searchId, string noLongerMatchingDonor, string? newlyMatchedDonor);

    Task RepeatSearchShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId, string searchId, string noLongerMatchingDonor, string? newlyMatchedDonor);
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

    public async Task<string> CreateDonor(ImportDonorType donorType)
    {
        return await donorImportSteps.CreateDonorWithSearchTestPhenotype(donorType);
    }

    public async Task EditDonorToNoLongerMatch(string donorCode, ImportDonorType donorType)
    {
        await donorImportSteps.EditDonorHlaToNonMatchingPhenotype(donorCode, donorType);
    }

    public async Task DeleteDonor(string donorCode)
    {
        await donorImportSteps.DeleteDonor(donorCode);
    }

    public async Task<string> OriginalSearchShouldReturnExpectedDonor(string searchRequestFileName, string expectedDonorCode)
    {
        var response = await searchTestSteps.SubmitSearchRequest(searchRequestFileName);
        await searchTestSteps.MatchingShouldHaveReturnedExpectedDonor(response.SearchIdentifier, expectedDonorCode);
        await searchTestSteps.SearchShouldHaveReturnedExpectedDonor(response.SearchIdentifier, expectedDonorCode);
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

    public async Task RepeatMatchingShouldHaveIdentifiedExpectedChanges(string repeatSearchId,
        string searchId,
        string? noLongerMatchingDonor,
        string? newlyMatchedDonor)
    {
        const string action = "Check repeat matching identifies expected changes";
        logger.LogStart(action);

        var notification = await FetchMatchingResultsNotification(repeatSearchId, searchId);
        notification.MatchingShouldHaveBeenSuccessful();

        var resultSetResponse = await workflow.FetchMatchingResultSet(notification.ToDebugSearchResultsRequest());
        logger.AssertResponseThenLogAndThrow(resultSetResponse, "Fetch matching result set");

        var matchingResultSet = resultSetResponse.DebugResult!;
        logger.AssertThenLogAndThrow(
            () => matchingResultSet.NoLongerMatchingDonors.Should().Contain(noLongerMatchingDonor),
            "Check for no longer matching donors");

        if (newlyMatchedDonor == null)
        {
            logger.LogCompletion(action);
            return;
        }

        var donorResult = matchingResultSet.GetDonorResult(newlyMatchedDonor);
        logger.AssertThenLogAndThrow(
            () => donorResult.Should().NotBeNull(),
            $"Select matching result for {newlyMatchedDonor}");

        await logger.AssertThenLogAndThrowAsync(
            () => VerifyJson(donorResult!.SerializeSingle())
                .IgnoreVaryingSearchResultProperties()
                .WriteReceivedToApprovalsFolder($"{testName}_MatchingResult"),
            "Matching result comparison to approved result");

        logger.LogCompletion(action);
    }

    public async Task RepeatSearchShouldHaveIdentifiedExpectedChanges(
        string repeatSearchId,
        string searchId,
        string? noLongerMatchingDonor,
        string? newlyMatchedDonor)
    {
        const string action = "Check repeat search identifies expected changes";
        logger.LogStart(action);

        var notificationResponse = await workflow.FetchSearchResultsNotification(repeatSearchId, searchId);
        logger.AssertResponseThenLogAndThrow(notificationResponse, "Fetch search results notification");

        var notification = notificationResponse.DebugResult!;
        notification.SearchShouldHaveBeenSuccessful();

        var resultSetResponse = await workflow.FetchSearchResultSet(notification.ToDebugSearchResultsRequest());
        logger.AssertResponseThenLogAndThrow(resultSetResponse, "Fetch search result set");

        var searchResultSet = resultSetResponse.DebugResult!;
        logger.AssertThenLogAndThrow(
            () => searchResultSet.NoLongerMatchingDonorCodes.Should().Contain(noLongerMatchingDonor),
            "Check for no longer matching donors");

        if (newlyMatchedDonor == null)
        {
            logger.LogCompletion(action);
            return;
        }

        var donorResult = searchResultSet.GetDonorResult(newlyMatchedDonor);
        logger.AssertThenLogAndThrow(
            () => donorResult.Should().NotBeNull(), 
            $"Select search result for {newlyMatchedDonor}");

        await logger.AssertThenLogAndThrowAsync(
            () => VerifyJson(donorResult!.SerializeSingle())
                .IgnoreVaryingSearchResultProperties()
                .WriteReceivedToApprovalsFolder($"{testName}_SearchResult"),
            "Search result comparison to approved result");

        logger.LogCompletion(action);
    }

    private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string repeatSearchId, string searchId)
    {
        var notificationResponse = await workflow.FetchMatchingResultsNotification(repeatSearchId, searchId);
        logger.AssertResponseThenLogAndThrow(notificationResponse, "Fetch matching results notification");
        return notificationResponse.DebugResult!;
    }
}