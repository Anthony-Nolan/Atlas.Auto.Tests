using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover happy paths of Atlas donor import when in diff mode.
/// </summary>
[TestFixture]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(DiffMode_HappyPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class DiffMode_HappyPathTests
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
    public async Task DonorImport_DiffMode_Create_CreatedDonorsSuccessfully()
    {
        const int donorCount = 2;
        const ImportDonorChangeType changeType = ImportDonorChangeType.Create;

        var updates = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(changeType)
            .Build(donorCount);

        var request = await donorImportWorkflow.ImportDiffDonorFile(updates);
        await donorImportWorkflow.DonorImportWasSuccessful(request.FileName, donorCount, 0);

        var expectedDonorInfo = updates.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await donorImportWorkflow.DonorsShouldBeAvailableForSearch(expectedDonorInfo);
    }

    [Test]
    public async Task DonorImport_DiffMode_Delete_DeletedDonorsSuccessfully()
    {
        const int updateCount = 1;

        // First have to create donor before it can be deleted
        var creationUpdate = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(updateCount);

        var creationRequest = await donorImportWorkflow.ImportDiffDonorFile(creationUpdate);
        await donorImportWorkflow.DonorImportWasSuccessful(creationRequest.FileName, updateCount, 0);

        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await donorImportWorkflow.DonorsShouldBeAvailableForSearch(expectedDonorInfo);

        // Now delete the existing donor
        var donorCode = creationUpdate.Select(d => d.RecordId).ToList();

        var deletionUpdate = DonorUpdateBuilder.New
            .WithRecordId(donorCode.Single())
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(updateCount);

        var deletionRequest = await donorImportWorkflow.ImportDiffDonorFile(deletionUpdate);
        await donorImportWorkflow.DonorImportWasSuccessful(deletionRequest.FileName, updateCount, 0);
        await donorImportWorkflow.DonorStoreShouldNotHaveTheseDonors(donorCode);
        await donorImportWorkflow.DonorsShouldNotBeAvailableForSearch(donorCode);
    }
}