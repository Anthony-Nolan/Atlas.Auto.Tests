using Atlas.Auto.Tests.TestHelpers.Assertions.DonorImport;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.DonorImport.FileSchema.Models;
using DonorImportRequest = Atlas.Auto.Tests.TestHelpers.Data.DonorImportRequest;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal class DonorImportTestSteps
{
    private const string NotifyOnUntrackedDonorDeletionSetting = "NotificationConfiguration:NotifyOnAttemptedDeletionOfUntrackedDonor";

    private readonly DonorImportWorkflow _workflow;
    public ILogger Logger { get; }

    public DonorImportTestSteps(IServiceProvider provider, ILogger logger)
    {
        _workflow = new DonorImportWorkflow(provider, logger);
        Logger = logger;
    }

    public async Task<DonorImportRequest> ImportDiffDonorFile(IEnumerable<DonorUpdate> updates)
    {
        var request = DonorImportRequestBuilder.New.WithDiffModeFile(updates).Build();
        await _workflow.ImportDonorFile(request);
        Logger.LogInformation("Sent donor import file {FileName}", request.FileName);
        return request;
    }

    public async Task<DonorImportRequest> ImportFullDonorFile(IEnumerable<DonorUpdate> updates)
    {
        var request = DonorImportRequestBuilder.New.WithFullModeFile(updates).Build();
        await _workflow.ImportDonorFile(request);
        Logger.LogInformation("Sent donor import file {FileName}", request.FileName);
        return request;
    }

    public async Task FullModeImportShouldNotBeAllowed()
    {
        var result = await _workflow.IsFullModeImportAllowed();
        result.Should().BeFalse("Full mode import should not be allowed");
    }

    public async Task DonorImportShouldHaveBeenSuccessful(
        string fileName, int expectedDonorCount, int expectedFailedDonorCount)
    {
        var result = await FetchDonorImportResultMessage(fileName);
        result.ImportShouldHaveBeenSuccessful();
        result.ShouldHaveImportedDonorCount(expectedDonorCount);
        result.ShouldHaveFailedDonorCount(expectedFailedDonorCount);
    }

    /// <summary>
    /// File-level checks: upload event, import result message, import history and the number of import failures.
    /// Check what the failures say with <see cref="FailedDonorUpdatesShouldHaveBeenLogged"/>.
    /// </summary>
    /// <returns>Upload time of the file, as recorded by Azure.</returns>
    public async Task<DateTimeOffset> DonorImportFileShouldHaveBeenProcessed(
        DonorImportRequest request, int expectedDonorCount, int expectedFailedDonorCount = 0)
    {
        var fileName = request.FileName;
        var uploadEvent = await _workflow.FetchFileUploadEvent(request);
        uploadEvent.Should().NotBeNull("File upload event should have been published for file {0}", fileName);
        (uploadEvent!.Data?.Url).Should().EndWith(fileName, "File upload event url should point to file {0}", fileName);

        await DonorImportShouldHaveBeenSuccessful(fileName, expectedDonorCount, expectedFailedDonorCount);

        var history = await _workflow.FetchFinishedDonorImportHistory(fileName);
        history.Should().NotBeNull("Donor import history should have been finished for file {0}", fileName);
        using (new AssertionScope())
        {
            history!.FileState.Should().Be(DonorImportHistoryRecord.CompletedState,
                "Import history state for file {0}", fileName);
            // FailureCount counts failed attempts to process the whole file, not failed donors
            history.FailureCount.Should().Be(0, "Import history failure count for file {0}", fileName);
            history.ImportedDonorsCount.Should().Be(expectedDonorCount, "Import history imported donor count for file {0}", fileName);
            history.FailedDonorCount.Should().Be(expectedFailedDonorCount, "Import history failed donor count for file {0}", fileName);
        }

        var failures = await _workflow.GetDonorImportFailures(fileName);
        failures.Should().HaveCount(expectedFailedDonorCount, "Donor import failures logged for file {0}", fileName);

        return uploadEvent.EventTime;
    }

    /// <returns>The current value, to give back to <see cref="RestoreUntrackedDonorDeletionNotifications"/>.</returns>
    public async Task<string?> GetUntrackedDonorDeletionNotifications()
    {
        var value = await _workflow.GetDonorImportSetting(NotifyOnUntrackedDonorDeletionSetting);
        Logger.LogInformation("{Setting:l} is '{Value:l}'", NotifyOnUntrackedDonorDeletionSetting, value);
        return value;
    }

    public async Task TurnOnUntrackedDonorDeletionNotifications(string? currentValue)
    {
        if (bool.TryParse(currentValue, out var isOn) && isOn)
            return;

        await SetDonorImportSettingAndWaitForRestart(bool.TrueString.ToLowerInvariant());
        Logger.LogInformation("{Setting:l} turned on", NotifyOnUntrackedDonorDeletionSetting);
    }

    /// <summary>
    /// Waits for the restart too, so the next tests do not upload files while the donor import function restarts.
    /// </summary>
    public async Task RestoreUntrackedDonorDeletionNotifications(string? originalValue)
    {
        var currentValue = await _workflow.GetDonorImportSetting(NotifyOnUntrackedDonorDeletionSetting);
        if (currentValue != originalValue)
        {
            await SetDonorImportSettingAndWaitForRestart(originalValue);
        }
        Logger.LogInformation("{Setting:l} restored to '{OriginalValue:l}'", NotifyOnUntrackedDonorDeletionSetting, originalValue);
    }

    private async Task SetDonorImportSettingAndWaitForRestart(string? value)
    {
        var restarted = await _workflow.SetDonorImportSetting(NotifyOnUntrackedDonorDeletionSetting, value);
        restarted.Should().BeTrue(
            "Donor import function should have restarted after setting {0} to '{1}'", NotifyOnUntrackedDonorDeletionSetting, value);
    }

    public async Task UntrackedDonorDeletionShouldHaveBeenNotified(string externalDonorCode, DateTimeOffset sentSince)
    {
        var notification = await _workflow.FetchUntrackedDonorDeletionNotification(externalDonorCode, sentSince);
        notification.Should().NotBeNull(
            "Untracked donor deletion notification should have been sent for donor {0}", externalDonorCode);

        // Only the untracked donor should be listed, not the donors that were deleted
        notification!.Description.Should().EndWith($"Donor ids: {externalDonorCode}.",
            "Notification should list only untracked donor {0}", externalDonorCode);
    }

    public async Task DonorImportShouldHaveFailed(string fileName)
    {
        var result = await FetchDonorImportResultMessage(fileName);
        result.ImportShouldHaveFailed();
    }

    public async Task CheckDonorStoreCount(IEnumerable<string> externalDonorCodes, int expectedCount)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        var result = await _workflow.CheckDonorsInDonorStore(codes, expectedCount);
        result.Should().NotBeNull(
            "Donor store should have {0} donor(s) from [{1}]", expectedCount, codeList);
    }

    /// <returns>Atlas ids of the donors, by external donor code.</returns>
    public async Task<IReadOnlyDictionary<string, int>> DonorStoreShouldHaveExpectedDonors(IReadOnlyCollection<DonorUpdate> expectedUpdates)
    {
        var codes = expectedUpdates.GetExternalDonorCodes();
        var result = await _workflow.CheckDonorsInDonorStore(codes, codes.Count);
        result.Should().NotBeNull(
            "Donor store should have donors [{0}]", string.Join(", ", codes));
        result.ShouldHaveExpectedDonors(expectedUpdates);
        return result!.PresentDonors.ToDictionary(d => d.ExternalDonorCode, d => d.AtlasId);
    }

    public async Task DonorLogsShouldExist(IReadOnlyCollection<string> externalDonorCodes, bool exist)
    {
        var expectedCount = exist ? externalDonorCodes.Count : 0;
        var logs = await _workflow.FetchDonorLogs(externalDonorCodes, expectedCount);
        logs.Should().NotBeNull(
            "Donor logs should have {0} record(s) for codes [{1}]", expectedCount, string.Join(", ", externalDonorCodes));
    }

    public async Task DonorUpdatesShouldHaveBeenPublished(IEnumerable<int> atlasIds, int expectedUpdatesPerDonor)
    {
        var donorIds = atlasIds.ToList();
        var updates = await _workflow.FetchPublishedDonorUpdates(donorIds, expectedUpdatesPerDonor);
        updates.Should().NotBeNull(
            "Each of donors [{0}] should have {1} published update(s) in PublishableDonorUpdates",
            string.Join(", ", donorIds), expectedUpdatesPerDonor);
        updates.Should().HaveCount(donorIds.Count * expectedUpdatesPerDonor);
    }

    public async Task SearchableDonorUpdatesShouldHaveBeenSent(
        IReadOnlyDictionary<string, int> atlasIdsByCode, DateTimeOffset publishedSince, bool isAvailableForSearch)
    {
        var donorIds = atlasIdsByCode.Values.ToList();
        var messages = await _workflow.FetchSearchableDonorUpdates(donorIds, publishedSince);
        messages.Should().NotBeNull(
            "Searchable donor updates should have been sent for donor ids [{0}]", string.Join(", ", donorIds));

        using (new AssertionScope())
        {
            messages!.Select(m => m.DonorId).Should().BeEquivalentTo(donorIds,
                "One searchable donor update per donor should have been sent");
            messages.Should().OnlyContain(m => m.IsAvailableForSearch == isAvailableForSearch,
                "IsAvailableForSearch should be {0} for all donors", isAvailableForSearch);

            if (isAvailableForSearch)
            {
                messages.Should().OnlyContain(
                    m => m.SearchableDonorInformation != null
                         && atlasIdsByCode.Contains(new KeyValuePair<string, int>(m.SearchableDonorInformation.ExternalDonorCode, m.DonorId)),
                    "Searchable donor updates should carry the external donor code of donors [{0}]",
                    string.Join(", ", atlasIdsByCode.Keys));
            }
        }
    }

    public async Task DonorManagementLogsShouldHaveBeenUpdated(IEnumerable<int> atlasIds, DateTimeOffset updatedSince)
    {
        var donorIds = atlasIds.ToList();
        var logs = await _workflow.FetchDonorManagementLogs(donorIds, updatedSince);
        logs.Should().NotBeNull(
            "Matching algorithm donor management logs for donor ids [{0}] should have been updated since {1:O}",
            string.Join(", ", donorIds), updatedSince);
    }

    public async Task DonorsShouldBeAvailableForSearch(IReadOnlyCollection<DonorUpdate> expectedUpdates)
    {
        var codes = expectedUpdates.GetExternalDonorCodes();
        var codeList = string.Join(", ", codes);
        var result = await _workflow.CheckDonorsAreAvailableForSearch(codes);
        result.Should().NotBeNull(
            "Matching algorithm should confirm donors [{0}] are available for search", codeList);
        result!.ShouldHaveExpectedDonors(expectedUpdates);
    }

    public async Task DonorsShouldNotBeAvailableForSearch(IReadOnlyCollection<string> externalDonorCodes)
    {
        var codeList = string.Join(", ", externalDonorCodes);
        var result = await _workflow.CheckDonorsAreNotAvailableForSearch(externalDonorCodes);
        result.Should().NotBeNull(
            "Matching algorithm should confirm donors [{0}] are NOT available for search", codeList);
        result!.ShouldNotHaveTheseDonors(externalDonorCodes);
    }

    /// <summary>
    /// Not in the donor store and no donor logs.
    /// </summary>
    public async Task DonorsShouldNotBeStored(IReadOnlyCollection<string> externalDonorCodes)
    {
        await CheckDonorStoreCount(externalDonorCodes, 0);
        await DonorLogsShouldExist(externalDonorCodes, exist: false);
    }

    /// <summary>
    /// Not stored and not available for search. Call it once matching has processed the file
    /// (a deletion, or a valid donor from the same file already available for search),
    /// otherwise the search check passes only because matching has not got there yet.
    /// </summary>
    public async Task DonorsShouldNotExist(IReadOnlyCollection<string> externalDonorCodes)
    {
        await DonorsShouldNotBeStored(externalDonorCodes);
        await DonorsShouldNotBeAvailableForSearch(externalDonorCodes);
    }

    public async Task MatchingAlgorithmDonorInfoShouldBe(IReadOnlyCollection<DonorUpdate> expectedUpdates)
    {
        var codeList = string.Join(", ", expectedUpdates.Select(d => d.RecordId));
        var result = await _workflow.CheckDonorInfoInMatchingAlgorithmIsAsExpected(expectedUpdates);
        result.Should().NotBeNull(
            "Donor info in matching algorithm should match expected for codes [{0}]", codeList);
    }

    public async Task FullModeImportAlertShouldHaveBeenRaised(string fileName)
    {
        var alert = await _workflow.FetchFailedFileAlert(fileName);
        alert.Should().NotBeNull(
            "File failure alert should have been received for file {0}", fileName);
        alert!.Summary.Should().ContainEquivalentOf("full mode is not allowed");
    }

    public async Task HlaExpansionFailureShouldBeReportedFor(string donorCode, string invalidHlaName)
    {
        var alert = await _workflow.FetchHlaExpansionFailureAlert();
        alert.Should().NotBeNull("HLA expansion failure alert should have been received");

        var failures = await _workflow.FetchHlaExpansionFailuresForDonor(donorCode);
        failures.Should().NotBeNull(
            "HLA expansion failures should have been reported for donor {0}", donorCode);
        failures!.ShouldContainFailureFor(donorCode, invalidHlaName);
    }

    public async Task FailedDonorUpdatesShouldHaveBeenLogged(
        string fileName, IEnumerable<DonorImportFailure> expectedFailures)
    {
        var failures = await _workflow.FetchDonorImportFailures(fileName);
        failures.Should().NotBeNull(
            "Donor import failures should have been logged for file {0}", fileName);

        using (new AssertionScope())
        {
            var expectedList = expectedFailures.ToList();
            failures!.Should().HaveCount(expectedList.Count,
                "Failed update count for file {0} should be {1} but was {2}",
                fileName, expectedList.Count, failures!.Count);
            failures.Should().BeEquivalentTo(expectedList,
                options => options.Excluding(f => f.Id).Excluding(f => f.UpdateFile).Excluding(f => f.FailureTime),
                "Failed updates for file {0} should match expected", fileName);
        }
    }

    private async Task<DonorImportMessage> FetchDonorImportResultMessage(string fileName)
    {
        var result = await _workflow.FetchResultMessage(fileName);
        result.Should().NotBeNull(
            "Import result message should have been received for file {0}", fileName);

        Logger.LogInformation(
            "Import result for {FileName}: WasSuccessful={WasSuccessful}, ImportedDonorCount={ImportedCount}, FailedDonorCount={FailedCount}",
            result!.FileName, result.WasSuccessful,
            result.SuccessfulImportInfo?.ImportedDonorCount,
            result.SuccessfulImportInfo?.FailedDonorCount);

        return result;
    }

}
