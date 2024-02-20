using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.Tests.DonorImport
{
    internal static class DonorImportWorkflowTestSteps
    {
        public static async Task<DonorImportRequest> ImportDiffDonorFile(
            this IDonorImportWorkflow workflow, 
            IEnumerable<DonorUpdate> updates)
        {
            var request = DonorImportRequestBuilder.New.WithDiffModeFile(updates).Build();

            var importResponse = await workflow.ImportDonorFile(request);
            importResponse.Should().BeTrue("the file should have been sent to Atlas");

            return request;
        }

        public static async Task<DonorImportRequest> ImportFullDonorFile(
            this IDonorImportWorkflow workflow,
            IEnumerable<DonorUpdate> updates)
        {
            var request = DonorImportRequestBuilder.New.WithFullModeFile(updates).Build();

            var importResponse = await workflow.ImportDonorFile(request);
            importResponse.Should().BeTrue("the file should have been sent to Atlas");

            return request;
        }

        public static async Task DonorImportWasSuccessful(
            this IDonorImportWorkflow workflow,
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

        public static async Task DonorStoreShouldHaveExpectedDonors(
            this IDonorImportWorkflow workflow,
            IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo)
        {
            var donorStoreCheckResponse = await workflow.CheckDonorsInDonorStore(expectedDonorInfo.GetExternalDonorCodes());
            donorStoreCheckResponse.ShouldBeSuccessful();

            var donorStoreCheckResult = donorStoreCheckResponse.DebugResult;
            donorStoreCheckResult?.ShouldHaveExpectedDonors(expectedDonorInfo);
        }

        public static async Task DonorsShouldBeAvailableForSearch(
            this IDonorImportWorkflow workflow,
            IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo)
        {
            var matchingDbCheckResponse = await workflow.CheckDonorsAreAvailableForSearch(expectedDonorInfo.GetExternalDonorCodes());
            matchingDbCheckResponse.ShouldBeSuccessful();

            var matchingDbCheckResult = matchingDbCheckResponse.DebugResult;
            matchingDbCheckResult?.ShouldHaveExpectedDonors(expectedDonorInfo);
        }

        public static async Task DonorStoreShouldNotHaveTheseDonors(
            this IDonorImportWorkflow workflow,
            IReadOnlyCollection<string> externalDonorCodes)
        {
            var donorStoreCheckResponse = await workflow.CheckDonorsInDonorStore(externalDonorCodes);
            donorStoreCheckResponse.ShouldBeSuccessful();

            var donorStoreCheckResult = donorStoreCheckResponse.DebugResult;
            donorStoreCheckResult?.ShouldNotHaveTheseDonors(externalDonorCodes);
        }

        public static async Task DonorsShouldNotBeAvailableForSearch(
            this IDonorImportWorkflow workflow,
            IReadOnlyCollection<string> externalDonorCodes)
        {
            var matchingDbCheckResponse = await workflow.CheckDonorsAreNotAvailableForSearch(externalDonorCodes);
            matchingDbCheckResponse.ShouldBeSuccessful();

            var matchingDbCheckResult = matchingDbCheckResponse.DebugResult;
            matchingDbCheckResult?.ShouldNotHaveTheseDonors(externalDonorCodes);
        }

        public static async Task DonorImportShouldHaveFailed(
            this IDonorImportWorkflow workflow,
            string fileName)
        {
            var fetchResultResponse = await workflow.FetchResultMessage(fileName);
            fetchResultResponse.ShouldBeSuccessful();
            fetchResultResponse.DebugResult?.ImportFailed();
        }

        public static async Task ShouldHaveRaisedAlertForFullModeImport(
            this IDonorImportWorkflow workflow,
            string fileName)
        {
            var fetchAlertResponse = await workflow.FetchFailedFileAlert(fileName);
            fetchAlertResponse.ShouldBeSuccessful();
            fetchAlertResponse.DebugResult?.ShouldSayFullModeImportNotAllowed();
        }

        public static async Task ShouldHaveRaisedAlertForHlaExpansionFailure(this IDonorImportWorkflow workflow)
        {
            var fetchAlertResponse = await workflow.FetchHlaExpansionFailureAlert();
            fetchAlertResponse.ShouldBeSuccessful();
            // no need to assert on the alert message contents, as it doesn't contain any data specific to our test
        }

        public static async Task ShouldHaveFailureInfo(
            this IDonorImportWorkflow workflow,
            string fileName,
            IEnumerable<FailedDonorUpdate> expectedFailedDonorInfo)
        {
            var fetchInfoResponse = await workflow.FetchDonorImportFailureInfo(fileName);
            fetchInfoResponse.ShouldBeSuccessful();
            fetchInfoResponse.DebugResult?.ShouldBeEquivalentTo(fileName, expectedFailedDonorInfo.ToList());
        }
    }
}