using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.Workflows;
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
    private IServiceProvider serviceProvider;
    private IDonorImportWorkflow donorImportWorkflow;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        serviceProvider = ServiceConfiguration.CreateProvider();
    }

    [SetUp]
    public void SetUp()
    {
        donorImportWorkflow = serviceProvider.ResolveServiceOrThrow<IDonorImportWorkflow>();
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;
        const ImportDonorChangeType changeType = ImportDonorChangeType.Create;

        // import the same donor create update twice

        // first time: both import and creation update should succeed
        var creationUpdate = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(changeType)
            .Build(donorCount);

        var firstImportRequest = await donorImportWorkflow.ImportDiffDonorFile(creationUpdate);
        await donorImportWorkflow.DonorImportWasSuccessful(firstImportRequest.FileName, donorCount, 0);

        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await donorImportWorkflow.DonorsShouldBeAvailableForSearch(expectedDonorInfo);

        // second time: the import should succeed, but the update should fail validation
        var secondImportRequest = await donorImportWorkflow.ImportDiffDonorFile(creationUpdate);
        await donorImportWorkflow.DonorImportWasSuccessful(secondImportRequest.FileName, 0, donorCount);
    }

    [Test]
    public async Task DonorImport_DiffMode_EditNonExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;

        var update = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Edit)
            .Build(donorCount);

        var request = await donorImportWorkflow.ImportDiffDonorFile(update);
        await donorImportWorkflow.DonorImportWasSuccessful(request.FileName, 0, donorCount);
        await donorImportWorkflow.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithMissingRequiredHla_FailsTheInvalidUpdate()
    {
        const int donorCount = 2;

        var update = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await donorImportWorkflow.ImportDiffDonorFile(update);
        await donorImportWorkflow.DonorImportWasSuccessful(request.FileName, 0, donorCount);
        await donorImportWorkflow.DonorStoreShouldNotHaveTheseDonors(update.GetExternalDonorCodes());
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateWithInvalidHla_DoesNotMakeDonorAvailableForSearch()
    {
        const int donorCount = 1;

        var creationUpdate = DonorUpdateBuilder.Default
            .WithInvalidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount);

        var creationRequest = await donorImportWorkflow.ImportDiffDonorFile(creationUpdate);
        await donorImportWorkflow.DonorImportWasSuccessful(creationRequest.FileName, donorCount, 0);

        // donor should have been created in donor store, but then failed to be made available for search
        // order of asserts matters: need to first check for the alert, and then check for search availability
        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await donorImportWorkflow.ShouldHaveRaisedAlertForHlaExpansionFailure();
        await donorImportWorkflow.DonorsShouldNotBeAvailableForSearch(expectedDonorInfo.GetExternalDonorCodes().ToList());
        //todo #19: this test is prone to false positive outcome until we can query application insights for the exact HLA expansion failure custom event
    }

    [Test]
    public async Task DonorImport_DiffMode_MixOfValidAndInvalidUpdates_AppliesTheValidAndFailsTheInvalidUpdates()
    {
        const int validDonorCount = 1;
        const int invalidDonorCount = 1;

        var validUpdate = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(validDonorCount);

        var invalidUpdate = DonorUpdateBuilder.Default
            .WithHlaAtEveryLocusExceptDrb1()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(invalidDonorCount);

        var request = await donorImportWorkflow.ImportDiffDonorFile(validUpdate.Concat(invalidUpdate));
        await donorImportWorkflow.DonorImportWasSuccessful(request.FileName, validDonorCount, invalidDonorCount);

        var expectValidInfo = validUpdate.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(expectValidInfo);
        await donorImportWorkflow.DonorsShouldBeAvailableForSearch(expectValidInfo);
        await donorImportWorkflow.DonorStoreShouldNotHaveTheseDonors(invalidUpdate.GetExternalDonorCodes());
    }
}