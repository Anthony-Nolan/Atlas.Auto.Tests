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
[Parallelizable(scope: ParallelScope.All)]
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

    /// <summary>
    /// Merged the various update types into a single test to reduce test execution time.
    /// </summary>
    [Test]
    public async Task DonorImport_DiffMode_CreateEditDelete_AppliesUpdates()
    {
        const int donorCount = 2;

        // Create donors via Create and Upsert
        var creationUpdates = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var creationRequest = await donorImportWorkflow.ImportDiffDonorFile(creationUpdates);
        await donorImportWorkflow.DonorImportWasSuccessful(creationRequest.FileName, donorCount, 0);

        var createdDonorInfo = creationUpdates.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(createdDonorInfo);
        await donorImportWorkflow.DonorsShouldBeAvailableForSearch(createdDonorInfo);

        var donorCodes = creationUpdates.GetExternalDonorCodes();

        // Update donors via Edit and Upsert
        var editUpdates = DonorUpdateBuilder.Default
            .WithAlternativeDnaAtLocusA()
            .WithChangeTypes(new[] { ImportDonorChangeType.Edit, ImportDonorChangeType.Upsert })
            .WithRecordIds(donorCodes)
            .Build(donorCount);

        var editRequest = await donorImportWorkflow.ImportDiffDonorFile(editUpdates);
        await donorImportWorkflow.DonorImportWasSuccessful(editRequest.FileName, donorCount, 0);

        var editedDonorInfo = editUpdates.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(editedDonorInfo);
        // Purposefully not checking if donors are available for search, as only changed the HLA,
        // and current approach depends on change in donor availability to determine when matching algorithm has caught up with donor updates.
        // todo #17: extend active matching db checker logic to check for changes in HLA.

        // Delete donors
        var deletionUpdates = DonorUpdateBuilder.New
            .WithRecordIds(donorCodes)
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var deletionRequest = await donorImportWorkflow.ImportDiffDonorFile(deletionUpdates);
        await donorImportWorkflow.DonorImportWasSuccessful(deletionRequest.FileName, donorCount, 0);
        await donorImportWorkflow.DonorStoreShouldNotHaveTheseDonors(donorCodes);
        await donorImportWorkflow.DonorsShouldNotBeAvailableForSearch(donorCodes);
    }

    /// <summary>
    /// Deleting a non-existing donor is not considered an exception path, as the final state is correct.
    /// </summary>
    [Test]
    public async Task DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate()
    {
        const int donorCount = 1;

        var update = DonorUpdateBuilder.New
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var request = await donorImportWorkflow.ImportDiffDonorFile(update);
        await donorImportWorkflow.DonorImportWasSuccessful(request.FileName, donorCount, 0);
    }
}