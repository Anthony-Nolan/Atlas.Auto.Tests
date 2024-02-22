using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
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

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        serviceProvider = ServiceConfiguration.CreateProvider();
    }

    /// <summary>
    /// Merged the various update types into a single test to reduce test execution time.
    /// </summary>
    [Test]
    public async Task DonorImport_DiffMode_CreateEditDelete_AppliesUpdates()
    {
        const int donorCount = 2;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        // Create donors via Create and Upsert
        var creationUpdates = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var creationRequest = await testSteps.ImportDiffDonorFile(creationUpdates);
        await testSteps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

        var createdDonorInfo = creationUpdates.ToDonorDebugInfo().ToList();
        await testSteps.DonorStoreShouldHaveExpectedDonors(createdDonorInfo);
        await testSteps.DonorsShouldBeAvailableForSearch(createdDonorInfo);

        var donorCodes = creationUpdates.GetExternalDonorCodes();

        // Update donors via Edit and Upsert
        var editUpdates = DonorUpdateBuilder.Default
            .WithAlternativeDnaAtLocusA()
            .WithChangeTypes(new[] { ImportDonorChangeType.Edit, ImportDonorChangeType.Upsert })
            .WithRecordIds(donorCodes)
            .Build(donorCount);

        var editRequest = await testSteps.ImportDiffDonorFile(editUpdates);
        await testSteps.DonorImportShouldHaveBeenSuccessful(editRequest.FileName, donorCount, 0);

        var editedDonorInfo = editUpdates.ToDonorDebugInfo().ToList();
        await testSteps.DonorStoreShouldHaveExpectedDonors(editedDonorInfo);
        // Purposefully not checking if donors are available for search, as only changed the HLA,
        // and current approach depends on change in donor availability to determine when matching algorithm has caught up with donor updates.
        // todo #17: extend active matching db checker logic to check for changes in HLA.

        // Delete donors
        var deletionUpdates = DonorUpdateBuilder.New
            .WithRecordIds(donorCodes)
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var deletionRequest = await testSteps.ImportDiffDonorFile(deletionUpdates);
        await testSteps.DonorImportShouldHaveBeenSuccessful(deletionRequest.FileName, donorCount, 0);
        await testSteps.DonorStoreShouldNotHaveTheseDonors(donorCodes);
        await testSteps.DonorsShouldNotBeAvailableForSearch(donorCodes);
    }

    /// <summary>
    /// Deleting a non-existing donor is not considered an exception path, as the final state is correct.
    /// </summary>
    [Test]
    public async Task DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate()
    {
        const int donorCount = 1;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        var update = DonorUpdateBuilder.New
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var request = await testSteps.ImportDiffDonorFile(update);
        await testSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);
    }
}