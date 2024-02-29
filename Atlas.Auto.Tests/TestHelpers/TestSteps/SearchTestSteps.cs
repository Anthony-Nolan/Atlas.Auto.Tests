using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Client.Models.Search.Requests;
using System.Text.Json;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.DonorImport.FileSchema.Models;

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
    }

    internal class SearchTestSteps : ISearchTestSteps
    {
        private readonly ISearchWorkflow workflow;
        private readonly IDonorImportTestSteps donorImportTestSteps;
        private readonly ITestLogger logger;

        public SearchTestSteps(
            ISearchWorkflow workflow,
            IDonorImportTestSteps donorImportTestSteps,
            ITestLogger logger)
        {
            this.workflow = workflow;
            this.donorImportTestSteps = donorImportTestSteps;
            this.logger = logger;
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

        private async Task<SearchInitiationResponse> SubmitSearch(string fileName)
        {
            const string action = "Submit search request";
            logger.LogInfo(action);

            var fileContents = await SourceDataReader.ReadJsonFile(fileName);
            var searchRequest = JsonSerializer.Deserialize<SearchRequest>(fileContents);
            var searchResponse = await workflow.SubmitSearchRequest(
                searchRequest ?? throw new InvalidOperationException("Search request was not read from source file."));
            logger.AssertResponseThenLogAndThrow(searchResponse, action);

            return searchResponse.DebugResult!;
        }
    }
}