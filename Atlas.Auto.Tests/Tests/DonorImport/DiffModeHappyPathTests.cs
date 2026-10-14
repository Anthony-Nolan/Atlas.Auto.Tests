using Atlas.Auto.Tests.TestHelpers.Attributes;
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

        var creationUpdates = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var creationRequest = await steps.ImportDiffDonorFile(creationUpdates);
        await steps.DonorImportShouldHaveBeenSuccessful(creationRequest.FileName, donorCount, 0);

        var createdDonorList = creationUpdates.ToList();
        await steps.CheckDonorStoreCount(creationUpdates.GetExternalDonorCodes(), donorCount);
        await steps.DonorsShouldBeAvailableForSearch(createdDonorList);

        var donorCodes = creationUpdates.GetExternalDonorCodes();

        var editUpdates = DonorUpdateBuilder.Default
            .WithAlternativeDnaAtLocusA()
            .WithChangeTypes(new[] { ImportDonorChangeType.Edit, ImportDonorChangeType.Upsert })
            .WithRecordIds(donorCodes)
            .Build(donorCount);

        var editRequest = await steps.ImportDiffDonorFile(editUpdates);
        await steps.DonorImportShouldHaveBeenSuccessful(editRequest.FileName, donorCount, 0);

        var editedDonorList = editUpdates.ToList();
        await steps.CheckDonorStoreCount(donorCodes, donorCount);
        await steps.MatchingAlgorithmDonorInfoShouldBe(editedDonorList);

        var deletionUpdates = DonorUpdateBuilder.New
            .WithRecordIds(donorCodes)
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var deletionRequest = await steps.ImportDiffDonorFile(deletionUpdates);
        await steps.DonorImportShouldHaveBeenSuccessful(deletionRequest.FileName, donorCount, 0);
        await steps.CheckDonorStoreCount(donorCodes, 0);
        await steps.DonorsShouldNotBeAvailableForSearch(donorCodes);
    }

    /// <summary>
    /// Create, delete and edit/upsert donors with HLA that needs expanding,
    /// checking each change through donor store, Service Bus and the active matching database.
    /// </summary>
    [Test]
    [ManualTest("QA-1926")]
    public async Task DonorImport_DiffMode_CreateDeleteUpsertWithExpandedHla_PropagatesUpdates()
    {
        const int donorCount = 3;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_CreateDeleteUpsertWithExpandedHla_PropagatesUpdates));

        // Create
        var creations = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotype()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(donorCount)
            .ToList();

        var creationRequest = await steps.ImportDiffDonorFile(creations);
        var creationUploadTime = await steps.DonorImportFileShouldHaveBeenProcessed(creationRequest, donorCount);

        var atlasIds = await steps.DonorStoreShouldHaveExpectedDonors(creations);
        await steps.DonorLogsShouldExist(creations.GetExternalDonorCodes(), exist: true);
        await steps.DonorUpdatesShouldHaveBeenPublished(atlasIds.Values, expectedUpdatesPerDonor: 1);
        await steps.SearchableDonorUpdatesShouldHaveBeenSent(atlasIds, creationUploadTime, isAvailableForSearch: true);
        await steps.DonorsShouldBeAvailableForSearch(creations);
        await steps.DonorManagementLogsShouldHaveBeenUpdated(atlasIds.Values, creationUploadTime);

        // Delete
        var deletedCode = creations[0].RecordId;
        var deletedDonor = new Dictionary<string, int> { [deletedCode] = atlasIds[deletedCode] };
        var deletions = DonorUpdateBuilder.New
            .WithRecordIds([deletedCode])
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(1)
            .ToList();

        var deletionRequest = await steps.ImportDiffDonorFile(deletions);
        var deletionUploadTime = await steps.DonorImportFileShouldHaveBeenProcessed(deletionRequest, 1);

        await steps.CheckDonorStoreCount([deletedCode], 0);
        await steps.DonorLogsShouldExist([deletedCode], exist: false);
        await steps.DonorUpdatesShouldHaveBeenPublished(deletedDonor.Values, expectedUpdatesPerDonor: 2);
        await steps.SearchableDonorUpdatesShouldHaveBeenSent(deletedDonor, deletionUploadTime, isAvailableForSearch: false);
        await steps.DonorsShouldNotBeAvailableForSearch([deletedCode]);
        await steps.DonorManagementLogsShouldHaveBeenUpdated(deletedDonor.Values, deletionUploadTime);

        // Edit existing donor and upsert a new one
        var editedCode = creations[1].RecordId;
        var edit = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotypeWithAlternativeA2()
            .WithRecordIds([editedCode])
            .WithChangeType(ImportDonorChangeType.Edit)
            .Build();
        var upsert = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotype()
            .WithChangeType(ImportDonorChangeType.Upsert)
            .Build();
        List<DonorUpdate> upserts = [edit, upsert];

        var upsertRequest = await steps.ImportDiffDonorFile(upserts);
        var upsertUploadTime = await steps.DonorImportFileShouldHaveBeenProcessed(upsertRequest, upserts.Count);

        var upsertedIds = await steps.DonorStoreShouldHaveExpectedDonors(upserts);
        await steps.DonorLogsShouldExist(upserts.GetExternalDonorCodes(), exist: true);
        // The edited donor keeps its creation update, the new one has only this one
        await steps.DonorUpdatesShouldHaveBeenPublished([atlasIds[editedCode]], expectedUpdatesPerDonor: 2);
        await steps.DonorUpdatesShouldHaveBeenPublished([upsertedIds[upsert.RecordId]], expectedUpdatesPerDonor: 1);
        await steps.SearchableDonorUpdatesShouldHaveBeenSent(upsertedIds, upsertUploadTime, isAvailableForSearch: true);
        await steps.MatchingAlgorithmDonorInfoShouldBe(upserts);
        await steps.DonorManagementLogsShouldHaveBeenUpdated(upsertedIds.Values, upsertUploadTime);
    }

    [Test]
    public async Task DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate()
    {
        const int donorCount = 1;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_DeleteNonExistingDonor_DoesNotFailTheUpdate));

        var update = DonorUpdateBuilder.New
            .WithChangeType(ImportDonorChangeType.Delete)
            .Build(donorCount);

        var request = await steps.ImportDiffDonorFile(update);
        await steps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);
    }
}
