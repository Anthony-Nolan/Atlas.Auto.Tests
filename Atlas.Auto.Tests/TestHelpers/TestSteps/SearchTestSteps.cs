using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Client.Models.Search.Requests;
using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Debug.Client.Models.Validation;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps
{
    /// <summary>
    /// Steps for testing Atlas search.
    /// Covers arrangement and execution of the search workflow and assertion of its outcomes.
    /// </summary>
    internal interface ISearchTestSteps
    {
        /// <summary>
        /// Creates a donor of the specified type and returns the record id.
        /// </summary>
        Task<string> CreateDonor(ImportDonorType donorType);

        Task<SearchInitiationResponse> SubmitSearchRequest(string searchRequestFileName);

        Task MatchingShouldHaveReturnedExpectedDonor(string searchRequestId, string expectedDonorCode);

        Task SearchShouldHaveReturnedExpectedDonor(string searchRequestId, string expectedDonorCode);

        Task<IEnumerable<RequestValidationFailure>> SubmitInvalidSearchRequest(string searchRequestFileName);

        Task MatchingShouldHaveFailedHlaValidation(string searchRequestId);
    }

    internal class SearchTestSteps : ISearchTestSteps
    {
        private readonly ISearchWorkflow workflow;
        private readonly IDonorImportStepsForSearchTests donorImportSteps;
        private readonly ITestLogger logger;
        private readonly string testName;

        public SearchTestSteps(
            ISearchWorkflow workflow,
            IDonorImportStepsForSearchTests donorImportSteps,
            ITestLogger logger,
            string testName)
        {
            this.workflow = workflow;
            this.donorImportSteps = donorImportSteps;
            this.logger = logger;
            this.testName = testName;
        }

        public async Task<string> CreateDonor(ImportDonorType donorType)
        {
            return await donorImportSteps.CreateDonorWithSearchTestPhenotype(donorType);
        }

        public async Task<SearchInitiationResponse> SubmitSearchRequest(string searchRequestFileName)
        {
            var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);
            var searchResponse = await workflow.SubmitSearchRequest(searchRequest);
            logger.AssertResponseThenLogAndThrow(searchResponse, "Submit valid search request");
            logger.LogInfo($"Search request id: {searchResponse.DebugResult!.SearchIdentifier}");
            return searchResponse.DebugResult!;
        }

        public async Task<IEnumerable<RequestValidationFailure>> SubmitInvalidSearchRequest(string searchRequestFileName)
        {
            var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(searchRequestFileName);
            var searchResponse = await workflow.SubmitInvalidSearchRequest(searchRequest);
            logger.AssertResponseThenLogAndThrow(searchResponse, "Submit invalid search request");
            return searchResponse.DebugResult!;
        }

        public async Task MatchingShouldHaveFailedHlaValidation(string searchRequestId)
        {
            var notification = await FetchMatchingResultsNotification(searchRequestId);
            notification.MatchingShouldHaveFailedHlaValidation();
        }

        public async Task MatchingShouldHaveReturnedExpectedDonor(string searchRequestId, string expectedDonorCode)
        {
            const string action = "Check matching returns expected donor";
            logger.LogStart(action);

            var notification = await FetchMatchingResultsNotification(searchRequestId);
            notification.MatchingShouldHaveBeenSuccessful();

            var resultSetResponse = await workflow.FetchMatchingResultSet(notification.ToDebugSearchResultsRequest());
            logger.AssertResponseThenLogAndThrow(resultSetResponse, "Fetch matching result set");

            var matchingResultSet = resultSetResponse.DebugResult!;
            var donorResult = matchingResultSet.GetDonorResult(expectedDonorCode);
            logger.AssertThenLogAndThrow(() => donorResult.Should().NotBeNull(), $"Select matching result for {expectedDonorCode}");

            await logger.AssertThenLogAndThrowAsync(
                () => VerifyJson(donorResult!.SerializeSingle())
                    .IgnoreVaryingSearchResultProperties()
                    .WriteReceivedToApprovalsFolder($"{testName}_MatchingResult"),
                "Matching result comparison to approved result");

            logger.LogCompletion(action);
        }

        public async Task SearchShouldHaveReturnedExpectedDonor(string searchRequestId, string expectedDonorCode)
        {
            const string action = "Check search returns expected donor";
            logger.LogStart(action);

            var notificationResponse = await workflow.FetchSearchResultsNotification(searchRequestId);
            logger.AssertResponseThenLogAndThrow(notificationResponse, "Fetch search results notification");

            var notification = notificationResponse.DebugResult!;
            notification.SearchShouldHaveBeenSuccessful();

            var resultSetResponse = await workflow.FetchSearchResultSet(notification.ToDebugSearchResultsRequest());
            logger.AssertResponseThenLogAndThrow(resultSetResponse, "Fetch search result set");

            var searchResultSet = resultSetResponse.DebugResult!;
            var donorResult = searchResultSet.GetDonorResult(expectedDonorCode);
            logger.AssertThenLogAndThrow(() => donorResult.Should().NotBeNull(), $"Select search result for {expectedDonorCode}");

            await logger.AssertThenLogAndThrowAsync(
                () => VerifyJson(donorResult!.SerializeSingle())
                    .IgnoreVaryingSearchResultProperties()
                    .WriteReceivedToApprovalsFolder($"{testName}_SearchResult"),
                "Search result comparison to approved result");

            logger.LogCompletion(action);
        }

        private async Task<MatchingResultsNotification> FetchMatchingResultsNotification(string searchRequestId)
        {
            var notificationResponse = await workflow.FetchMatchingResultsNotification(searchRequestId);
            logger.AssertResponseThenLogAndThrow(notificationResponse, "Fetch matching results notification");
            return notificationResponse.DebugResult!;
        }
    }
}