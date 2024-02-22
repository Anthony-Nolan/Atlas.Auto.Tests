using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover exception paths of Atlas donor import when in diff mode.
/// </summary>
[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(DiffMode_ExceptionPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class DiffMode_ExceptionPathTests
{
    private const string RecordIdProp = "RecordId";
    private const string Drb1DnaProp = "Hla.DRB1.Dna";
    private const string Drb1FailureReason = "Required locus Drb1: minimum HLA typing has not been provided";

    private IServiceProvider serviceProvider;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        serviceProvider = ServiceConfiguration.CreateProvider();
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        // import the same donor create update twice

        // first time: both import and creation update should succeed
        var creationUpdate = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var firstImportRequest = await testSteps.ImportDiffDonorFile(creationUpdate);
        await testSteps.DonorImportShouldHaveBeenSuccessful(firstImportRequest.FileName, donorCount, 0);

        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await testSteps.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await testSteps.DonorsShouldBeAvailableForSearch(expectedDonorInfo);

        // second time: the import should succeed, but the update should fail validation
        var secondImportRequest = await testSteps.ImportDiffDonorFile(creationUpdate);
        await testSteps.DonorImportShouldHaveBeenSuccessful(secondImportRequest.FileName, 0, donorCount);
        await testSteps.FailedDonorUpdatesShouldHaveBeenLogged(
            secondImportRequest.FileName,
            creationUpdate.ToFailureInfo(RecordIdProp, "Donor is already present in the database."));
    }

    [Test]
    public async Task DonorImport_DiffMode_EditNonExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        var update = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Edit)
            .Build(donorCount);

        var request = await testSteps.ImportDiffDonorFile(update);
        await testSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, 0, donorCount);
        await testSteps.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
        await testSteps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToFailureInfo(RecordIdProp, "Donor is not present in the database."));
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithMissingRequiredHla_FailsTheInvalidUpdate()
    {
        const int donorCount = 2;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        var update = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await testSteps.ImportDiffDonorFile(update);
        await testSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, 0, donorCount);
        await testSteps.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
        await testSteps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToFailureInfo(Drb1DnaProp, Drb1FailureReason));
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithInvalidHla_DoesNotMakeDonorAvailableForSearch()
    {
        const int donorCount = 1;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        var creationUpdate = DonorUpdateBuilder.Default
            .WithInvalidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var creationRequest = await testSteps.ImportDiffDonorFile(creationUpdate);
        await testSteps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

        // donor should have been created in donor store, but then failed to be made available for search
        // order of asserts matters: need to first check for the alert, and then check for search availability
        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await testSteps.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await testSteps.HlaExpansionFailureAlertShouldHaveBeenRaised();
        await testSteps.DonorsShouldNotBeAvailableForSearch(expectedDonorInfo.GetExternalDonorCodes().ToList());
        //todo #19: this test is prone to false positive outcome until we can query application insights for the exact HLA expansion failure custom event
    }

    [Test]
    public async Task DonorImport_DiffMode_MixOfValidAndInvalidUpdates_AppliesTheValidAndFailsTheInvalidUpdates()
    {
        const int validDonorCount = 1;
        const int invalidDonorCount = 1;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        var validUpdate = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(validDonorCount);

        var invalidUpdate = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(invalidDonorCount);

        var request = await testSteps.ImportDiffDonorFile(validUpdate.Concat(invalidUpdate));
        await testSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, validDonorCount, invalidDonorCount);

        var expectValidInfo = validUpdate.ToDonorDebugInfo().ToList();
        await testSteps.DonorStoreShouldHaveExpectedDonors(expectValidInfo);
        await testSteps.DonorsShouldBeAvailableForSearch(expectValidInfo);
        await testSteps.DonorStoreShouldNotHaveTheseDonors(invalidUpdate.GetExternalDonorCodes());
        await testSteps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            invalidUpdate.ToFailureInfo(Drb1DnaProp, Drb1FailureReason));
    }
}