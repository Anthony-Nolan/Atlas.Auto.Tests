using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

[TestFixture]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(DiffModeHappyPathTests)}")]
internal class DiffModeHappyPathTests : DonorImportTestBase
{
    public DiffModeHappyPathTests() : base(nameof(DiffModeHappyPathTests))
    {
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateEditDelete_AppliesUpdates()
    {
        const int donorCount = 2;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_CreateEditDelete_AppliesUpdates));

        var currentTestCase = "create donors via Create and Upsert";
        steps.Logger.LogStart(currentTestCase);

        var creationUpdates = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var creationRequest = await steps.ImportDiffDonorFile(creationUpdates);
        await steps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

        var createdDonorInfo = creationUpdates.ToDonorDebugInfo().ToList();
        await steps.DonorStoreShouldHaveExpectedDonors(createdDonorInfo);
        await steps.DonorsShouldBeAvailableForSearch(createdDonorInfo);

        steps.Logger.LogCompletion(currentTestCase);

        var donorCodes = creationUpdates.GetExternalDonorCodes();

        currentTestCase = "update donors via Edit and Upsert";
        steps.Logger.LogStart(currentTestCase);

        var editUpdates = DonorUpdateBuilder.Default
            .WithAlternativeDnaAtLocusA()
            .WithChangeTypes(new[] { ImportDonorChangeType.Edit, ImportDonorChangeType.Upsert })
            .WithRecordIds(donorCodes)
            .Build(donorCount);

        var editRequest = await steps.ImportDiffDonorFile(editUpdates);
        await steps.DonorImportShouldHaveBeenSuccessful(editRequest.FileName, donorCount, 0);

        var editedDonorInfo = editUpdates.ToDonorDebugInfo().ToList();
        await steps.DonorStoreShouldHaveExpectedDonors(editedDonorInfo);
        await steps.MatchingAlgorithmDonorInfoShouldBe(editedDonorInfo);
        steps.Logger.LogCompletion(currentTestCase);

        currentTestCase = "delete donors";
        steps.Logger.LogStart(currentTestCase);

        var deletionUpdates = DonorUpdateBuilder.New
            .WithRecordIds(donorCodes)
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var deletionRequest = await steps.ImportDiffDonorFile(deletionUpdates);
        await steps.DonorImportShouldHaveBeenSuccessful(deletionRequest.FileName, donorCount, 0);
        await steps.DonorStoreShouldNotHaveTheseDonors(donorCodes);
        await steps.DonorsShouldNotBeAvailableForSearch(donorCodes);

        steps.Logger.LogCompletion(currentTestCase);
    }

    [Test]
    public async Task DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate()
    {
        const string testCase = "deletion of non-existing donor";
        const int donorCount = 1;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate));

        steps.Logger.LogStart(testCase);

        var update = DonorUpdateBuilder.New
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var request = await steps.ImportDiffDonorFile(update);
        await steps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);

        steps.Logger.LogCompletion(testCase);
    }
}
