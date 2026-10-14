using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class DonorImportStepsForSearchTests
{
    private readonly DonorImportTestSteps _donorImportTestSteps;
    private readonly ILogger _logger;

    public DonorImportStepsForSearchTests(DonorImportTestSteps donorImportTestSteps, ILogger logger)
    {
        _donorImportTestSteps = donorImportTestSteps;
        _logger = logger;
    }

    public async Task<string> CreateDonor(ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
    {
        _logger.LogInformation($"Create test {donorType}");

        const int donorCount = 1;
        var donorUpdate = DonorUpdateBuilder.Default
            .WithDonorType(donorType)
            .WithHla(hlaBuilder)
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var request = await _donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
        await _donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);

        var donorInfo = donorUpdate.ToDonorDebugInfo().ToList();
        await _donorImportTestSteps.DonorStoreShouldHaveExpectedDonors(donorInfo);
        await _donorImportTestSteps.DonorsShouldBeAvailableForSearch(donorInfo);

        var recordId = donorUpdate.Single().RecordId;
        _logger.LogInformation($"Donor record id: {recordId}");

        return recordId;
    }

    public async Task EditDonorHla(string donorCode, ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
    {
        _logger.LogInformation($"Edit HLA of test {donorType} with record id {donorCode}");

        const int donorCount = 1;
        var donorUpdate = DonorUpdateBuilder.Default
            .WithRecordIds(new[] { donorCode })
            .WithDonorType(donorType)
            .WithHla(hlaBuilder)
            .WithChangeType(ImportDonorChangeType.Edit)
            .Build(donorCount);

        var request = await _donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
        await _donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);

        var donorInfo = donorUpdate.ToDonorDebugInfo().ToList();
        await _donorImportTestSteps.DonorStoreShouldHaveExpectedDonors(donorInfo);
        await _donorImportTestSteps.MatchingAlgorithmDonorInfoShouldBe(donorInfo);
    }

    public async Task DeleteDonors(IReadOnlyCollection<string> donorCodes)
    {
        var donorUpdate = DonorUpdateBuilder.Default
            .WithRecordIds(donorCodes)
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCodes.Count);

        var request = await _donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
        await _donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCodes.Count, 0);
        await _donorImportTestSteps.DonorStoreShouldNotHaveTheseDonors(donorCodes);
        await _donorImportTestSteps.DonorsShouldNotBeAvailableForSearch(donorCodes);
    }
}
