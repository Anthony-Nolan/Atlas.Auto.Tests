using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

[TestFixture]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(DiffModeExceptionPathTests)}")]
internal class DiffModeExceptionPathTests : DonorImportTestBase
{
    private const string RecordIdProp = "RecordId";
    private const string Drb1DnaProp = "Hla.DRB1.Dna";
    private const string Drb1FailureReason = "Required locus Drb1: minimum HLA typing has not been provided";

    public DiffModeExceptionPathTests() : base(nameof(DiffModeExceptionPathTests))
    {
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_CreateExistingDonor_FailsTheInvalidUpdate));

        var creationUpdate = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var firstImportRequest = await steps.ImportDiffDonorFile(creationUpdate);
        await steps.DonorImportShouldHaveBeenSuccessful(firstImportRequest.FileName, donorCount, 0);

        var donorList = creationUpdate.ToList();
        await steps.CheckDonorStoreCount(creationUpdate.GetExternalDonorCodes(), donorCount);
        await steps.DonorsShouldBeAvailableForSearch(donorList);

        var secondImportRequest = await steps.ImportDiffDonorFile(creationUpdate);
        await steps.DonorImportShouldHaveBeenSuccessful(secondImportRequest.FileName, 0, donorCount);
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            secondImportRequest.FileName,
            creationUpdate.ToExpectedFailures(RecordIdProp, "Donor is already present in the database."));
    }

    [Test]
    public async Task DonorImport_DiffMode_EditNonExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_EditNonExistingDonor_FailsTheInvalidUpdate));

        var update = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeType(ImportDonorChangeType.Edit)
            .Build(donorCount);

        var request = await steps.ImportDiffDonorFile(update);
        await steps.DonorImportShouldHaveBeenSuccessful(request.FileName, 0, donorCount);
        await steps.CheckDonorStoreCount(update.GetExternalDonorCodes(), 0);
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToExpectedFailures(RecordIdProp, "Donor is not present in the database."));
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithMissingRequiredHla_FailsTheInvalidUpdate()
    {
        const int donorCount = 2;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_CreateWithMissingRequiredHla_FailsTheInvalidUpdate));

        var update = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await steps.ImportDiffDonorFile(update);
        await steps.DonorImportShouldHaveBeenSuccessful(request.FileName, 0, donorCount);
        await steps.CheckDonorStoreCount(update.GetExternalDonorCodes(), 0);
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToExpectedFailures(Drb1DnaProp, Drb1FailureReason));
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithInvalidHla_ReportsInvalidHla_AndDoesNotMakeDonorAvailableForSearch()
    {
        const int donorCount = 1;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_CreateWithInvalidHla_ReportsInvalidHla_AndDoesNotMakeDonorAvailableForSearch));

        var creationUpdate = DonorUpdateBuilder.Default
            .WithInvalidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var creationRequest = await steps.ImportDiffDonorFile(creationUpdate);
        await steps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

        var donorList = creationUpdate.ToList();
        var donorCodes = creationUpdate.GetExternalDonorCodes();
        await steps.CheckDonorStoreCount(donorCodes, donorCount);
        await steps.HlaExpansionFailureShouldBeReportedFor(donorCodes.Single(), HlaTypings.InvalidDnaForAnyLocus);
        await steps.DonorsShouldNotBeAvailableForSearch(donorCodes);
    }

    [Test]
    public async Task DonorImport_DiffMode_MixOfValidAndInvalidUpdates_AppliesTheValidAndFailsTheInvalidUpdates()
    {
        const int validDonorCount = 1;
        const int invalidDonorCount = 1;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_MixOfValidAndInvalidUpdates_AppliesTheValidAndFailsTheInvalidUpdates));

        var validUpdate = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(validDonorCount);

        var invalidUpdate = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(invalidDonorCount);

        var request = await steps.ImportDiffDonorFile(validUpdate.Concat(invalidUpdate));
        await steps.DonorImportShouldHaveBeenSuccessful(request.FileName, validDonorCount, invalidDonorCount);

        var validList = validUpdate.ToList();
        await steps.CheckDonorStoreCount(validUpdate.GetExternalDonorCodes(), validDonorCount);
        await steps.DonorsShouldBeAvailableForSearch(validList);
        await steps.CheckDonorStoreCount(invalidUpdate.GetExternalDonorCodes(), 0);
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            invalidUpdate.ToExpectedFailures(Drb1DnaProp, Drb1FailureReason));
    }
}
