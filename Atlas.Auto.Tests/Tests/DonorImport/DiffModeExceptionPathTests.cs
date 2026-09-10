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

        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await steps.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await steps.DonorsShouldBeAvailableForSearch(expectedDonorInfo);

        var secondImportRequest = await steps.ImportDiffDonorFile(creationUpdate);
        await steps.DonorImportShouldHaveBeenSuccessful(secondImportRequest.FileName, 0, donorCount);
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            secondImportRequest.FileName,
            creationUpdate.ToFailureInfo(RecordIdProp, "Donor is already present in the database."));
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
        await steps.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToFailureInfo(RecordIdProp, "Donor is not present in the database."));
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
        await steps.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToFailureInfo(Drb1DnaProp, Drb1FailureReason));
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

        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        var donorCode = expectedDonorInfo.GetExternalDonorCodes().ToList();
        await steps.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await steps.HlaExpansionFailureShouldBeReportedFor(donorCode.Single(), HlaTypings.InvalidDnaForAnyLocus);
        await steps.DonorsShouldNotBeAvailableForSearch(donorCode);
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

        var expectValidInfo = validUpdate.ToDonorDebugInfo().ToList();
        await steps.DonorStoreShouldHaveExpectedDonors(expectValidInfo);
        await steps.DonorsShouldBeAvailableForSearch(expectValidInfo);
        await steps.DonorStoreShouldNotHaveTheseDonors(invalidUpdate.GetExternalDonorCodes());
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            invalidUpdate.ToFailureInfo(Drb1DnaProp, Drb1FailureReason));
    }
}
