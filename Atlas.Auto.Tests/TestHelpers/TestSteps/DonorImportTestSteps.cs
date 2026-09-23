using Atlas.Auto.Tests.TestHelpers.Assertions.DonorImport;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.DonorImport.FileSchema.Models;
using DonorImportRequest = Atlas.Auto.Tests.TestHelpers.Data.DonorImportRequest;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class DonorImportTestSteps
{
    private readonly DonorImportWorkflow _workflow;
    public ILogger Logger { get; }

    public DonorImportTestSteps(IServiceProvider provider, ILogger logger)
    {
        _workflow = new DonorImportWorkflow(provider);
        Logger = logger;
    }

    public async Task<DonorImportRequest> ImportDiffDonorFile(IEnumerable<DonorUpdate> updates)
    {
        var request = DonorImportRequestBuilder.New.WithDiffModeFile(updates).Build();
        await _workflow.ImportDonorFile(request);
        Logger.LogInformation($"Sent donor import file {request.FileName}");
        return request;
    }

    public async Task<DonorImportRequest> ImportFullDonorFile(IEnumerable<DonorUpdate> updates)
    {
        var request = DonorImportRequestBuilder.New.WithFullModeFile(updates).Build();
        await _workflow.ImportDonorFile(request);
        Logger.LogInformation($"Sent donor import file {request.FileName}");
        return request;
    }

    public async Task FullModeImportShouldNotBeAllowed()
    {
        var result = await _workflow.IsFullModeImportAllowed();
        result.Should().BeFalse("Full mode import should not be allowed");
    }

    public async Task DonorImportShouldHaveBeenSuccessful(
        string fileName, int expectedDonorCount, int expectedFailedDonorCount)
    {
        var result = await FetchDonorImportResultMessage(fileName);
        result.ImportShouldHaveBeenSuccessful();
        result.ShouldHaveImportedDonorCount(expectedDonorCount);
        result.ShouldHaveFailedDonorCount(expectedFailedDonorCount);
    }

    public async Task DonorImportShouldHaveFailed(string fileName)
    {
        var result = await FetchDonorImportResultMessage(fileName);
        result.ImportShouldHaveFailed();
    }

    public async Task DonorStoreShouldHaveExpectedDonors(IReadOnlyCollection<DonorUpdate> expectedUpdates)
    {
        var codes = expectedUpdates.GetExternalDonorCodes().ToList();
        var donorCheck = await CheckDonorStore(codes);
        donorCheck.ShouldHaveExpectedDonors(expectedUpdates);
    }

    public async Task DonorStoreShouldNotHaveTheseDonors(IReadOnlyCollection<string> externalDonorCodes)
    {
        var donorCheck = await CheckDonorStore(externalDonorCodes);
        donorCheck.ShouldNotHaveTheseDonors(externalDonorCodes);
    }

    public async Task DonorsShouldBeAvailableForSearch(IReadOnlyCollection<DonorUpdate> expectedUpdates)
    {
        var codes = expectedUpdates.GetExternalDonorCodes();
        var codeList = string.Join(", ", codes);
        var result = await _workflow.CheckDonorsAreAvailableForSearch(codes);
        result.Should().NotBeNull(
            "Matching algorithm should confirm donors [{0}] are available for search", codeList);
        result!.ShouldHaveExpectedDonors(expectedUpdates);
    }

    public async Task DonorsShouldNotBeAvailableForSearch(IReadOnlyCollection<string> externalDonorCodes)
    {
        var codeList = string.Join(", ", externalDonorCodes);
        var result = await _workflow.CheckDonorsAreNotAvailableForSearch(externalDonorCodes);
        result.Should().NotBeNull(
            "Matching algorithm should confirm donors [{0}] are NOT available for search", codeList);
        result!.ShouldNotHaveTheseDonors(externalDonorCodes);
    }

    public async Task MatchingAlgorithmDonorInfoShouldBe(IReadOnlyCollection<DonorUpdate> expectedUpdates)
    {
        var codeList = string.Join(", ", expectedUpdates.Select(d => d.RecordId));
        var result = await _workflow.CheckDonorInfoInMatchingAlgorithmIsAsExpected(expectedUpdates);
        result.Should().NotBeNull(
            "Donor info in matching algorithm should match expected for codes [{0}]", codeList);
    }

    public async Task FullModeImportAlertShouldHaveBeenRaised(string fileName)
    {
        var alert = await _workflow.FetchFailedFileAlert(fileName);
        alert.Should().NotBeNull(
            "File failure alert should have been received for file {0}", fileName);
        alert!.Summary.Should().ContainEquivalentOf("full mode is not allowed");
    }

    public async Task HlaExpansionFailureShouldBeReportedFor(string donorCode, string invalidHlaName)
    {
        var alert = await _workflow.FetchHlaExpansionFailureAlert();
        alert.Should().NotBeNull("HLA expansion failure alert should have been received");

        var failures = await _workflow.FetchHlaExpansionFailuresForDonor(donorCode);
        failures.Should().NotBeNull(
            "HLA expansion failures should have been reported for donor {0}", donorCode);
        failures!.ShouldContainFailureFor(donorCode, invalidHlaName);
    }

    public async Task FailedDonorUpdatesShouldHaveBeenLogged(
        string fileName, IEnumerable<DonorImportFailure> expectedFailures)
    {
        var failures = await _workflow.FetchDonorImportFailures(fileName);
        failures.Should().NotBeNull(
            "Donor import failures should have been logged for file {0}", fileName);
        var expectedList = expectedFailures.ToList();
        failures!.Should().HaveCount(expectedList.Count,
            "Failed update count for file {0} should be {1} but was {2}",
            fileName, expectedList.Count, failures!.Count);
        failures.Should().BeEquivalentTo(expectedList,
            options => options.Excluding(f => f.Id).Excluding(f => f.UpdateFile).Excluding(f => f.FailureTime),
            "Failed updates for file {0} should match expected", fileName);
    }

    private async Task<DonorImportMessage> FetchDonorImportResultMessage(string fileName)
    {
        var result = await _workflow.FetchResultMessage(fileName);
        result.Should().NotBeNull(
            "Import result message should have been received for file {0}", fileName);
        return result!;
    }

    private async Task<DonorCheckResult<Donor>> CheckDonorStore(IReadOnlyCollection<string> donorCodes)
    {
        var codeList = string.Join(", ", donorCodes);
        var result = await _workflow.CheckDonorsInDonorStore(donorCodes);
        result.Should().NotBeNull(
            "Donor store check should have returned a result for codes [{0}]", codeList);
        return result!;
    }
}
