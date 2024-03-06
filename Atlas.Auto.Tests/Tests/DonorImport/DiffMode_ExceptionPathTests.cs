using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover exception paths of Atlas donor import when in diff mode.
/// </summary>
[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(DiffMode_ExceptionPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class DiffMode_ExceptionPathTests : DonorImportTestBase
{
    private const string RecordIdProp = "RecordId";
    private const string Drb1DnaProp = "Hla.DRB1.Dna";
    private const string Drb1FailureReason = "Required locus Drb1: minimum HLA typing has not been provided";

    public DiffMode_ExceptionPathTests() : base(nameof(DiffMode_ExceptionPathTests))
    {
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;
        var test = GetTestServices(nameof(DonorImport_DiffMode_CreateExistingDonor_FailsTheInvalidUpdate));

        var currentTestCase = "create donor in diff mode";
        test.Logger.LogStart(currentTestCase);

        var creationUpdate = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var firstImportRequest = await test.Steps.ImportDiffDonorFile(creationUpdate);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(firstImportRequest.FileName, donorCount, 0);

        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await test.Steps.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await test.Steps.DonorsShouldBeAvailableForSearch(expectedDonorInfo);

        test.Logger.LogCompletion(currentTestCase);

        currentTestCase = "repeat create of donor in diff mode";
        test.Logger.LogStart(currentTestCase);

        var secondImportRequest = await test.Steps.ImportDiffDonorFile(creationUpdate);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(secondImportRequest.FileName, 0, donorCount);
        await test.Steps.FailedDonorUpdatesShouldHaveBeenLogged(
            secondImportRequest.FileName,
            creationUpdate.ToFailureInfo(RecordIdProp, "Donor is already present in the database."));

        test.Logger.LogCompletion(currentTestCase);
    }

    [Test]
    public async Task DonorImport_DiffMode_EditNonExistingDonor_FailsTheInvalidUpdate()
    {
        const string testCase = "edit non-existing donor in diff mode";
        const int donorCount = 1;
        var test = GetTestServices(nameof(DonorImport_DiffMode_EditNonExistingDonor_FailsTheInvalidUpdate));

        test.Logger.LogStart(testCase);

        var update = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeType(ImportDonorChangeType.Edit)
            .Build(donorCount);

        var request = await test.Steps.ImportDiffDonorFile(update);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(request.FileName, 0, donorCount);
        await test.Steps.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
        await test.Steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToFailureInfo(RecordIdProp, "Donor is not present in the database."));

        test.Logger.LogCompletion(testCase);
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithMissingRequiredHla_FailsTheInvalidUpdate()
    {
        const string testCase = "create donor with missing required HLA in diff mode";
        const int donorCount = 2;
        var test = GetTestServices(nameof(DonorImport_DiffMode_CreateWithMissingRequiredHla_FailsTheInvalidUpdate));

        test.Logger.LogStart(testCase);

        var update = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await test.Steps.ImportDiffDonorFile(update);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(request.FileName, 0, donorCount);
        await test.Steps.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
        await test.Steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToFailureInfo(Drb1DnaProp, Drb1FailureReason));

        test.Logger.LogCompletion(testCase);
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithInvalidHla_DoesNotMakeDonorAvailableForSearch()
    {
        const string testCase = "create donor with invalid HLA in diff mode";
        const int donorCount = 1;
        var test = GetTestServices(nameof(DonorImport_DiffMode_CreateWithInvalidHla_DoesNotMakeDonorAvailableForSearch));

        test.Logger.LogStart(testCase);

        var creationUpdate = DonorUpdateBuilder.Default
            .WithInvalidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var creationRequest = await test.Steps.ImportDiffDonorFile(creationUpdate);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

        // donor should have been created in donor store, but then failed to be made available for search
        // order of asserts matters: need to first check for the alert, and then check for search availability
        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await test.Steps.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await test.Steps.HlaExpansionFailureAlertShouldHaveBeenRaised();
        await test.Steps.DonorsShouldNotBeAvailableForSearch(expectedDonorInfo.GetExternalDonorCodes().ToList());
        //todo #19: this test is prone to false positive outcome until we can query application insights for the exact HLA expansion failure custom event

        test.Logger.LogCompletion(testCase);
    }

    [Test]
    public async Task DonorImport_DiffMode_MixOfValidAndInvalidUpdates_AppliesTheValidAndFailsTheInvalidUpdates()
    {
        const string testCase = "create mix of valid and invalid donors in diff mode";
        const int validDonorCount = 1;
        const int invalidDonorCount = 1;
        var test = GetTestServices(nameof(DonorImport_DiffMode_MixOfValidAndInvalidUpdates_AppliesTheValidAndFailsTheInvalidUpdates));

        test.Logger.LogStart(testCase);

        var validUpdate = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(validDonorCount);

        var invalidUpdate = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(invalidDonorCount);

        var request = await test.Steps.ImportDiffDonorFile(validUpdate.Concat(invalidUpdate));
        await test.Steps.DonorImportShouldHaveBeenSuccessful(request.FileName, validDonorCount, invalidDonorCount);

        var expectValidInfo = validUpdate.ToDonorDebugInfo().ToList();
        await test.Steps.DonorStoreShouldHaveExpectedDonors(expectValidInfo);
        await test.Steps.DonorsShouldBeAvailableForSearch(expectValidInfo);
        await test.Steps.DonorStoreShouldNotHaveTheseDonors(invalidUpdate.GetExternalDonorCodes());
        await test.Steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            invalidUpdate.ToFailureInfo(Drb1DnaProp, Drb1FailureReason));

        test.Logger.LogCompletion(testCase);
    }
}