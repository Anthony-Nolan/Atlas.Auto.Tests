using Atlas.Auto.Tests.TestHelpers.Attributes;
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
    private const string A1DnaProp = "Hla.A.Dna.Field1";
    private const string A1FailureReason = "Required locus A, Dna: Field1 cannot be empty";

    public DiffModeExceptionPathTests() : base(nameof(DiffModeExceptionPathTests))
    {
    }

    /// <summary>
    /// QA-2907: a file creating 1 existing and 2 new donors imports the new donors and fails only the existing one,
    /// which keeps its original data.
    /// </summary>
    [Test]
    [ManualTest("QA-1928")]
    [AutoTest("QA-2907")]
    public async Task DonorImport_DiffMode_CreateExistingDonor_FailsOnlyThatDonor()
    {
        const int newDonorCount = 2;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_CreateExistingDonor_FailsOnlyThatDonor));

        // Precondition: import the donor that will already exist
        var existingDonor = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotype()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build();
        var precondition = await steps.ImportDiffDonorFile([existingDonor]);
        await steps.DonorImportFileShouldHaveBeenProcessed(precondition, 1);
        await steps.DonorsShouldBeAvailableForSearch([existingDonor]);

        // Create the existing donor again, with different HLA, plus 2 new donors
        var existingDonorAgain = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotypeWithAlternativeA2()
            .WithRecordIds([existingDonor.RecordId])
            .WithChangeType(ImportDonorChangeType.Create)
            .Build();
        var newDonors = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotype()
            .WithChangeType(ImportDonorChangeType.Create)
            .Build(newDonorCount)
            .ToList();
        var request = await steps.ImportDiffDonorFile([existingDonorAgain, .. newDonors]);
        var uploadTime = await steps.DonorImportFileShouldHaveBeenProcessed(request, newDonorCount, expectedFailedDonorCount: 1);
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            [existingDonorAgain.ToExpectedFailure(RecordIdProp, "Donor is already present in the database.")]);

        // All 3 donors are stored; the existing one keeps its original HLA
        List<DonorUpdate> expectedDonors = [existingDonor, .. newDonors];
        var atlasIds = await steps.DonorStoreShouldHaveExpectedDonors(expectedDonors);
        await steps.DonorLogsShouldExist(expectedDonors.GetExternalDonorCodes(), exist: true);
        // The failed update must not publish a second update for the existing donor
        await steps.DonorUpdatesShouldHaveBeenPublished(atlasIds.Values, expectedUpdatesPerDonor: 1);

        // Only the new donors are sent to matching
        var newDonorIds = atlasIds
            .Where(d => d.Key != existingDonor.RecordId)
            .ToDictionary(d => d.Key, d => d.Value);
        await steps.SearchableDonorUpdatesShouldHaveBeenSent(newDonorIds, uploadTime, isAvailableForSearch: true);
        await steps.DonorsShouldBeAvailableForSearch(expectedDonors);
        await steps.DonorManagementLogsShouldHaveBeenUpdated(newDonorIds.Values, uploadTime);
    }

    /// <summary>
    /// QA-2908: a file deleting 3 existing donors and 1 donor Atlas has never had deletes them all
    /// and sends a notification about the untracked donor.
    /// </summary>
    /// <remarks>
    /// The notification is off by default on some environments, so the test turns it on and restores it afterwards.
    /// Changing an app setting restarts the donor import function, which would break other tests' imports: hence non-parallel.
    /// </remarks>
    [Test]
    [NonParallelizable]
    [ManualTest("QA-1928")]
    [AutoTest("QA-2908")]
    public async Task DonorImport_DiffMode_DeleteUntrackedDonor_SendsNotification()
    {
        const int existingDonorCount = 3;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_DeleteUntrackedDonor_SendsNotification));

        // Remember the setting before changing it, so it is restored even if turning it on fails
        var originalSetting = await steps.GetUntrackedDonorDeletionNotifications();
        try
        {
            await steps.TurnOnUntrackedDonorDeletionNotifications(originalSetting);

            // Precondition: import the donors that will be deleted
            var existingDonors = DonorUpdateBuilder.Default
                .WithExpandedHlaPhenotype()
                .WithChangeType(ImportDonorChangeType.Create)
                .Build(existingDonorCount)
                .ToList();
            var precondition = await steps.ImportDiffDonorFile(existingDonors);
            await steps.DonorImportFileShouldHaveBeenProcessed(precondition, existingDonorCount);
            await steps.DonorsShouldBeAvailableForSearch(existingDonors);

            // Delete the existing donors plus one record id that was never imported
            var untrackedCode = DonorUpdateBuilder.New.Build().RecordId;
            List<string> deletedCodes = [.. existingDonors.GetExternalDonorCodes(), untrackedCode];
            var deletions = DonorUpdateBuilder.New
                .WithRecordIds(deletedCodes)
                .WithChangeType(ImportDonorChangeType.Delete)
                .Build(deletedCodes.Count)
                .ToList();
            var request = await steps.ImportDiffDonorFile(deletions);

            // Deleting an untracked donor is not a failure, only a notification
            await steps.DonorImportFileShouldHaveBeenProcessed(request, deletedCodes.Count);
            await steps.UntrackedDonorDeletionShouldHaveBeenNotified(untrackedCode, request.UploadedAt);

            // The existing donors are still deleted
            await steps.DonorsShouldNotExist(existingDonors.GetExternalDonorCodes());
        }
        finally
        {
            await steps.RestoreUntrackedDonorDeletionNotifications(originalSetting);
        }
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
        await steps.DonorsShouldNotBeStored(update.GetExternalDonorCodes());
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToExpectedFailures(RecordIdProp, "Donor is not present in the database."));
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
        await steps.DonorsShouldNotBeStored(update.GetExternalDonorCodes());
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            update.ToExpectedFailures(Drb1DnaProp, Drb1FailureReason));
    }

    [Test]
    [RequiresAppInsights]
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

        var donorList = creationUpdate.ToList();
        var donorCodes = creationUpdate.GetExternalDonorCodes();
        await steps.CheckDonorStoreCount(donorCodes, donorCount);
        await steps.HlaExpansionFailureShouldBeReportedFor(donorCodes.Single(), HlaTypings.InvalidDnaForAnyLocus);
        await steps.DonorsShouldNotBeAvailableForSearch(donorCodes);
    }

    /// <summary>
    /// QA-2909: a file upserting a valid donor and a donor with an empty first field at required locus A
    /// imports only the valid donor.
    /// </summary>
    [Test]
    [ManualTest("QA-1928")]
    [AutoTest("QA-2909")]
    public async Task DonorImport_DiffMode_DonorWithoutRequiredLocus_IsNotImported()
    {
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DiffMode_DonorWithoutRequiredLocus_IsNotImported));

        var validDonor = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotype()
            .WithChangeType(ImportDonorChangeType.Upsert)
            .Build();
        var invalidDonor = DonorUpdateBuilder.Default
            .WithExpandedHlaPhenotypeWithEmptyA1()
            .WithChangeType(ImportDonorChangeType.Upsert)
            .Build();
        var request = await steps.ImportDiffDonorFile([validDonor, invalidDonor]);

        // The file is processed: 1 donor imported, 1 failed because of locus A
        var uploadTime = await steps.DonorImportFileShouldHaveBeenProcessed(request, 1, expectedFailedDonorCount: 1);
        await steps.FailedDonorUpdatesShouldHaveBeenLogged(
            request.FileName,
            [invalidDonor.ToExpectedFailure(A1DnaProp, A1FailureReason)]);

        // Only the valid donor is stored, published and available for search
        var atlasIds = await steps.DonorStoreShouldHaveExpectedDonors([validDonor]);
        await steps.DonorLogsShouldExist([validDonor.RecordId], exist: true);
        await steps.DonorUpdatesShouldHaveBeenPublished(atlasIds.Values, expectedUpdatesPerDonor: 1);
        await steps.DonorsShouldBeAvailableForSearch([validDonor]);
        await steps.DonorManagementLogsShouldHaveBeenUpdated(atlasIds.Values, uploadTime);
        await steps.DonorsShouldNotExist([invalidDonor.RecordId]);
    }
}
