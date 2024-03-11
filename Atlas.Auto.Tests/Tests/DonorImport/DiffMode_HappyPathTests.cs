using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover happy paths of Atlas donor import when in diff mode.
/// </summary>
[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(DiffMode_HappyPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class DiffMode_HappyPathTests : DonorImportTestBase
{
    public DiffMode_HappyPathTests() : base(nameof(DiffMode_HappyPathTests))
    {
    }

    /// <summary>
    /// Merged the various update types into a single test to reduce test execution time.
    /// </summary>
    [Test]
    public async Task DonorImport_DiffMode_CreateEditDelete_AppliesUpdates()
    {
        const int donorCount = 2;
        var test = GetTestServices(nameof(DonorImport_DiffMode_CreateEditDelete_AppliesUpdates));

        var currentTestCase = "create donors via Create and Upsert";
        test.Logger.LogStart(currentTestCase);

        var creationUpdates = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var creationRequest = await test.Steps.ImportDiffDonorFile(creationUpdates);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

        var createdDonorInfo = creationUpdates.ToDonorDebugInfo().ToList();
        await test.Steps.DonorStoreShouldHaveExpectedDonors(createdDonorInfo);
        await test.Steps.DonorsShouldBeAvailableForSearch(createdDonorInfo);

        test.Logger.LogCompletion(currentTestCase);

        var donorCodes = creationUpdates.GetExternalDonorCodes();

        currentTestCase = "update donors via Edit and Upsert";
        test.Logger.LogStart(currentTestCase);

        var editUpdates = DonorUpdateBuilder.Default
            .WithAlternativeDnaAtLocusA()
            .WithChangeTypes(new[] { ImportDonorChangeType.Edit, ImportDonorChangeType.Upsert })
            .WithRecordIds(donorCodes)
            .Build(donorCount);

        var editRequest = await test.Steps.ImportDiffDonorFile(editUpdates);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(editRequest.FileName, donorCount, 0);

        var editedDonorInfo = editUpdates.ToDonorDebugInfo().ToList();
        await test.Steps.DonorStoreShouldHaveExpectedDonors(editedDonorInfo);
        await test.Steps.MatchingAlgorithmDonorInfoShouldBe(editedDonorInfo);
        test.Logger.LogCompletion(currentTestCase);

        currentTestCase = "delete donors";
        test.Logger.LogStart(currentTestCase);

        var deletionUpdates = DonorUpdateBuilder.New
            .WithRecordIds(donorCodes)
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var deletionRequest = await test.Steps.ImportDiffDonorFile(deletionUpdates);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(deletionRequest.FileName, donorCount, 0);
        await test.Steps.DonorStoreShouldNotHaveTheseDonors(donorCodes);
        await test.Steps.DonorsShouldNotBeAvailableForSearch(donorCodes);

        test.Logger.LogCompletion(currentTestCase);
    }

    /// <summary>
    /// Deleting a non-existing donor is not considered an exception path, as the final state is correct.
    /// </summary>
    [Test]
    public async Task DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate()
    {
        const string testCase = "deletion of non-existing donor";
        const int donorCount = 1;
        var test = GetTestServices(nameof(DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate));

        test.Logger.LogStart(testCase);

        var update = DonorUpdateBuilder.New
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var request = await test.Steps.ImportDiffDonorFile(update);
        await test.Steps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);

        test.Logger.LogCompletion(testCase);
    }
}