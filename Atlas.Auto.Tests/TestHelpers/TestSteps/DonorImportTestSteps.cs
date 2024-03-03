using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.Assertions.DonorImport;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
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
        private readonly ITestLogger logger;

        public DonorImportTestSteps(IDonorImportWorkflow workflow, ITestLogger logger)
        {
            this.workflow = workflow;
            this.logger = logger;
        }

        public async Task<DonorImportRequest> ImportDiffDonorFile(IEnumerable<DonorUpdate> updates)
        {
            var request = DonorImportRequestBuilder.New.WithDiffModeFile(updates).Build();
            await SendDonorImportFile(request);
            return request;
        }

        public async Task<DonorImportRequest> ImportFullDonorFile(IEnumerable<DonorUpdate> updates)
        {
            var request = DonorImportRequestBuilder.New.WithFullModeFile(updates).Build();
            await SendDonorImportFile(request);
            return request;
        }

        public async Task FullModeImportShouldNotBeAllowed()
        {
            var response = await workflow.IsFullModeImportAllowed();

            // The debug http response should have been successful,
            // but the embedded result should be false as full mode import should not be allowed.
            logger.AssertThenLogAndThrow(() =>
            {
                response.ShouldBeSuccessful();
                response.DebugResult.Should().BeFalse();
            }, "Check full mode import is **not** allowed");
        }

        public async Task DonorImportShouldHaveBeenSuccessful(
            string fileName,
            int expectedDonorCount,
            int expectedFailedDonorCount)
        {
            var resultMessage = await FetchDonorImportResultMessage(fileName);
            logger.AssertThenLogAndThrow(() =>
            {
                resultMessage.ImportShouldHaveBeenSuccessful();
                resultMessage?.ShouldHaveImportedDonorCount(expectedDonorCount);
                resultMessage?.ShouldHaveFailedDonorCount(expectedFailedDonorCount);
            }, $"Import of {resultMessage?.FileName}");
        }

        public async Task DonorImportShouldHaveFailed(string fileName)
        {
            var resultMessage = await FetchDonorImportResultMessage(fileName);
            logger.AssertThenLogAndThrow(resultMessage.ImportShouldHaveFailed, $"Rejection of file {fileName}");
        }

        public async Task DonorStoreShouldHaveExpectedDonors(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo)
        {
            var donorCheck = await CheckDonorStore(expectedDonorInfo.GetExternalDonorCodes());
            logger.AssertThenLogAndThrow(
                () => donorCheck.ShouldHaveExpectedDonors(expectedDonorInfo), 
                "Check for donor presence");
        }

        public async Task DonorStoreShouldNotHaveTheseDonors(IReadOnlyCollection<string> externalDonorCodes)
        {
            var donorCheck = await CheckDonorStore(externalDonorCodes);
            logger.AssertThenLogAndThrow(
                () => donorCheck.ShouldNotHaveTheseDonors(externalDonorCodes), 
                "Check for donor absence");
        }

        public async Task DonorsShouldBeAvailableForSearch(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo)
        {
            var donorCheck = await workflow.CheckDonorsAreAvailableForSearch(expectedDonorInfo.GetExternalDonorCodes());
            logger.AssertResponseThenLogAndThrow(donorCheck, "Search availability request");
            logger.AssertThenLogAndThrow(
                () => donorCheck.DebugResult.ShouldHaveExpectedDonors(expectedDonorInfo), 
                "Check that donors are searchable");
        }

        public async Task DonorsShouldNotBeAvailableForSearch(IReadOnlyCollection<string> externalDonorCodes)
        {
            var donorCheck = await workflow.CheckDonorsAreNotAvailableForSearch(externalDonorCodes);
            logger.AssertResponseThenLogAndThrow(donorCheck, "Search availability request");
            logger.AssertThenLogAndThrow(
                () => donorCheck.DebugResult.ShouldNotHaveTheseDonors(externalDonorCodes), 
                "Check that donors are **not** searchable");
        }

        public async Task FullModeImportAlertShouldHaveBeenRaised(string fileName)
        {
            var alertResponse = await workflow.FetchFailedFileAlert(fileName);
            logger.AssertResponseThenLogAndThrow(alertResponse, "Fetch file failure alert");
            logger.AssertThenLogAndThrow(
                () => alertResponse.DebugResult?.ShouldSayFullModeImportNotAllowed(), 
                "Alert for Full mode file import");
        }

        public async Task HlaExpansionFailureAlertShouldHaveBeenRaised()
        {
            var alertResponse = await workflow.FetchHlaExpansionFailureAlert();
            logger.AssertResponseThenLogAndThrow(alertResponse, "Fetch HLA expansion failure alert");
            // no need to assert on the alert message contents, as it doesn't contain any data specific to our test
        }

        public async Task FailedDonorUpdatesShouldHaveBeenLogged(string fileName, IEnumerable<FailedDonorUpdate> expectedFailedDonorInfo)
        {
            var infoResponse = await workflow.FetchDonorImportFailureInfo(fileName);
            logger.AssertResponseThenLogAndThrow(infoResponse, "Fetch donor import failures");
            logger.AssertThenLogAndThrow(
                () => infoResponse.DebugResult?.ShouldBeEquivalentTo(fileName, expectedFailedDonorInfo.ToList()), 
                "Logging of donor import failures");
        }

        private async Task SendDonorImportFile(DonorImportRequest request)
        {
            var importResponse = await workflow.ImportDonorFile(request);
            logger.AssertThenLogAndThrow(
                () => importResponse.Should().BeTrue(), 
                $"Send donor import file {request.FileName}");
        }

        private async Task<DonorImportMessage?> FetchDonorImportResultMessage(string fileName)
        {
            var response = await workflow.FetchResultMessage(fileName);
            logger.AssertResponseThenLogAndThrow(response, "Fetch donor import result message");
            return response.DebugResult;
        }

        private async Task<DebugDonorsResult?> CheckDonorStore(IEnumerable<string> donorCodes)
        {
            var response = await workflow.CheckDonorsInDonorStore(donorCodes);
            logger.AssertResponseThenLogAndThrow(response, "Check donor store request");
            return response.DebugResult;
        }
    }
}