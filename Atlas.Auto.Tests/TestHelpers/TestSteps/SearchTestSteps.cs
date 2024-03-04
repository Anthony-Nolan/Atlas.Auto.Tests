using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Client.Models.Search.Requests;
using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
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
        /// Creates a test donor of the specified type and returns the record ID of the created donor.
        /// </summary>
        Task<string> CreateTestDonor(ImportDonorType donorType);

        Task<SearchInitiationResponse> Submit10Of10DonorSearchRequest();

        Task MatchingShouldHaveReturnedExpectedDonor(string searchRequestId, string expectedDonorCode);

        Task SearchShouldHaveReturnedExpectedDonor(string searchRequestId, string expectedDonorCode);
    }

    internal class SearchTestSteps : ISearchTestSteps
    {
        private readonly ISearchWorkflow workflow;
        private readonly IDonorImportTestSteps donorImportTestSteps;
        private readonly ITestLogger logger;
        private readonly string testName;

        public SearchTestSteps(
            ISearchWorkflow workflow,
            IDonorImportTestSteps donorImportTestSteps,
            ITestLogger logger,
            string testName)
        {
            this.workflow = workflow;
            this.donorImportTestSteps = donorImportTestSteps;
            this.logger = logger;
            this.testName = testName;
        }

        public async Task<string> CreateTestDonor(ImportDonorType donorType)
        {
            const string action = "Create test donor";
            logger.LogStart(action);

            const int donorCount = 1;
            var donorUpdate = DonorUpdateBuilder.Default
                .WithSearchTestPhenotype()
                .WithChangeType(ImportDonorChangeType.Create)
                .WithDonorType(donorType)
                .Build(donorCount);

            var creationRequest = await donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
            await donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

            var createdDonorInfo = donorUpdate.ToDonorDebugInfo().ToList();
            await donorImportTestSteps.DonorStoreShouldHaveExpectedDonors(createdDonorInfo);
            await donorImportTestSteps.DonorsShouldBeAvailableForSearch(createdDonorInfo);

            var donorRecordId = donorUpdate.GetExternalDonorCodes().Single();
            logger.LogInfo($"Donor record id: {donorRecordId}");
            logger.LogCompletion(action);

            return donorRecordId;
        }

        public async Task<SearchInitiationResponse> Submit10Of10DonorSearchRequest()
        {
            const string fileName = "search-request-donor-10_10.json";
            var searchResponse = await SubmitSearch(fileName);
            logger.LogInfo($"Search request id: {searchResponse.SearchIdentifier}");
            return searchResponse;
        }

        public async Task MatchingShouldHaveReturnedExpectedDonor(string searchRequestId, string expectedDonorCode)
        {
            const string action = "Check matching returns expected donor";
            logger.LogStart(action);

            var notificationResponse = await workflow.FetchMatchingResultsNotification(searchRequestId);
            logger.AssertResponseThenLogAndThrow(notificationResponse, "Fetch matching results notification");

            var notification = notificationResponse.DebugResult!;
            notification.MatchingShouldHaveBeenSuccessful();

            var resultSetResponse = await workflow.FetchMatchingResultSet(notification.ResultsFileName, notification.BatchFolderName);
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

            var resultSetResponse = await workflow.FetchSearchResultSet(notification.ResultsFileName, notification.BatchFolderName);
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

        private async Task<SearchInitiationResponse> SubmitSearch(string fileName)
        {
            var searchRequest = await SourceDataReader.ReadJsonFile<SearchRequest>(fileName);
            var searchResponse = await workflow.SubmitSearchRequest(searchRequest);
            logger.AssertResponseThenLogAndThrow(searchResponse, "Submit search request");

            return searchResponse.DebugResult!;
        }
    }
}