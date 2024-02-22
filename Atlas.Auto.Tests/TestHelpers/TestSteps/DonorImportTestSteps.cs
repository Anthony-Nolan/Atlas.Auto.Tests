using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps
{
    /// <summary>
    /// Steps for testing Atlas donor import.
    /// Covers arrangement and execution of the donor import workflow and assertion of its outcomes.
    /// </summary>
    internal interface IDonorImportTestSteps
    {
        Task<DonorImportRequest> ImportDiffDonorFile(IEnumerable<DonorUpdate> updates);
        Task<DonorImportRequest> ImportFullDonorFile(IEnumerable<DonorUpdate> updates);

        Task FullModeImportShouldNotBeAllowed();

        Task DonorImportShouldHaveBeenSuccessful(string fileName, int expectedDonorCount, int expectedFailedDonorCount);
        Task DonorImportShouldHaveFailed(string fileName);

        Task DonorStoreShouldHaveExpectedDonors(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo);
        Task DonorStoreShouldNotHaveTheseDonors(IReadOnlyCollection<string> externalDonorCodes);

        Task DonorsShouldBeAvailableForSearch(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo);
        Task DonorsShouldNotBeAvailableForSearch(IReadOnlyCollection<string> externalDonorCodes);

        Task FullModeImportAlertShouldHaveBeenRaised(string fileName);
        Task HlaExpansionFailureAlertShouldHaveBeenRaised();

        Task FailedDonorUpdatesShouldHaveBeenLogged(string fileName, IEnumerable<FailedDonorUpdate> expectedFailedDonorInfo);
    }

    internal class DonorImportTestSteps : IDonorImportTestSteps
    {
        private readonly IDonorImportWorkflow workflow;

        public DonorImportTestSteps(IDonorImportWorkflow workflow)
        {
            this.workflow = workflow;
        }

        public async Task<DonorImportRequest> ImportDiffDonorFile(IEnumerable<DonorUpdate> updates)
        {
            var request = DonorImportRequestBuilder.New.WithDiffModeFile(updates).Build();

            var importResponse = await workflow.ImportDonorFile(request);
            importResponse.Should().BeTrue("the file should have been sent to Atlas");

            return request;
        }

        public async Task<DonorImportRequest> ImportFullDonorFile(IEnumerable<DonorUpdate> updates)
        {
            var request = DonorImportRequestBuilder.New.WithFullModeFile(updates).Build();

            var importResponse = await workflow.ImportDonorFile(request);
            importResponse.Should().BeTrue("the file should have been sent to Atlas");

            return request;
        }

        public async Task FullModeImportShouldNotBeAllowed()
        {
            var response = await workflow.IsFullModeImportAllowed();

            // The debug http response should have been successful,
            // but the embedded result should be false as full mode import should not be allowed.
            response.WasSuccess.Should().BeTrue();
            response.DebugResult.Should().BeFalse();
        }

        public async Task DonorImportShouldHaveBeenSuccessful(
            string fileName,
            int expectedDonorCount,
            int expectedFailedDonorCount)
        {
            var fetchResultResponse = await workflow.FetchResultMessage(fileName);
            fetchResultResponse.ShouldBeSuccessful();

            var importResultMessage = fetchResultResponse.DebugResult;
            importResultMessage?.ImportWasSuccessful();
            importResultMessage?.ShouldHaveImportedDonorCount(expectedDonorCount);
            importResultMessage?.ShouldHaveFailedDonorCount(expectedFailedDonorCount);
        }

        public async Task DonorImportShouldHaveFailed(string fileName)
        {
            var fetchResultResponse = await workflow.FetchResultMessage(fileName);
            fetchResultResponse.ShouldBeSuccessful();
            fetchResultResponse.DebugResult?.ImportFailed();
        }

        public async Task DonorStoreShouldHaveExpectedDonors(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo)
        {
            var donorStoreCheckResponse = await workflow.CheckDonorsInDonorStore(expectedDonorInfo.GetExternalDonorCodes());
            donorStoreCheckResponse.ShouldBeSuccessful();

            var donorStoreCheckResult = donorStoreCheckResponse.DebugResult;
            donorStoreCheckResult?.ShouldHaveExpectedDonors(expectedDonorInfo);
        }

        public async Task DonorStoreShouldNotHaveTheseDonors(IReadOnlyCollection<string> externalDonorCodes)
        {
            var donorStoreCheckResponse = await workflow.CheckDonorsInDonorStore(externalDonorCodes);
            donorStoreCheckResponse.ShouldBeSuccessful();

            var donorStoreCheckResult = donorStoreCheckResponse.DebugResult;
            donorStoreCheckResult?.ShouldNotHaveTheseDonors(externalDonorCodes);
        }

        public async Task DonorsShouldBeAvailableForSearch(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo)
        {
            var matchingDbCheckResponse = await workflow.CheckDonorsAreAvailableForSearch(expectedDonorInfo.GetExternalDonorCodes());
            matchingDbCheckResponse.ShouldBeSuccessful();

            var matchingDbCheckResult = matchingDbCheckResponse.DebugResult;
            matchingDbCheckResult?.ShouldHaveExpectedDonors(expectedDonorInfo);
        }

        public async Task DonorsShouldNotBeAvailableForSearch(IReadOnlyCollection<string> externalDonorCodes)
        {
            var matchingDbCheckResponse = await workflow.CheckDonorsAreNotAvailableForSearch(externalDonorCodes);
            matchingDbCheckResponse.ShouldBeSuccessful();

            var matchingDbCheckResult = matchingDbCheckResponse.DebugResult;
            matchingDbCheckResult?.ShouldNotHaveTheseDonors(externalDonorCodes);
        }

        public async Task FullModeImportAlertShouldHaveBeenRaised(string fileName)
        {
            var fetchAlertResponse = await workflow.FetchFailedFileAlert(fileName);
            fetchAlertResponse.ShouldBeSuccessful();
            fetchAlertResponse.DebugResult?.ShouldSayFullModeImportNotAllowed();
        }

        public async Task HlaExpansionFailureAlertShouldHaveBeenRaised()
        {
            var fetchAlertResponse = await workflow.FetchHlaExpansionFailureAlert();
            fetchAlertResponse.ShouldBeSuccessful();
            // no need to assert on the alert message contents, as it doesn't contain any data specific to our test
        }

        public async Task FailedDonorUpdatesShouldHaveBeenLogged(string fileName, IEnumerable<FailedDonorUpdate> expectedFailedDonorInfo)
        {
            var fetchInfoResponse = await workflow.FetchDonorImportFailureInfo(fileName);
            fetchInfoResponse.ShouldBeSuccessful();
            fetchInfoResponse.DebugResult?.ShouldBeEquivalentTo(fileName, expectedFailedDonorInfo.ToList());
        }
    }
}