using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.DonorImport.FileSchema.Models;
using DonorImportRequest = Atlas.Auto.Tests.TestHelpers.Data.DonorImportRequest;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

internal class DonorImportWorkflow
{
    private readonly FunctionAppHelper _functionAppHelper;
    private readonly AzureResourceSettings _azureResource;
    private readonly IDonorStoreSqlHelper _donorSqlHelper;
    private readonly AppInsightsHelper _appInsightsHelper;
    private readonly BlobStorageHelper _blobHelper;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly ILogger _logger;
    private readonly NotificationFetcher<BlobCreatedEvent> _fileUploadFetcher;
    private readonly NotificationFetcher<DonorImportMessage> _importResultFetcher;
    private readonly NotificationFetcher<SearchableDonorUpdate> _searchableDonorUpdateFetcher;
    private readonly NotificationFetcher<Alert> _alertFetcher;

    public DonorImportWorkflow(IServiceProvider provider, ILogger logger)
    {
        _functionAppHelper = provider.GetRequiredService<FunctionAppHelper>();
        _azureResource = provider.GetRequiredService<AzureResourceSettings>();
        _appInsightsHelper = provider.GetRequiredService<AppInsightsHelper>();
        _donorSqlHelper = provider.GetRequiredService<IDonorStoreSqlHelper>();
        _blobHelper = provider.GetRequiredService<BlobStorageHelper>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        _logger = logger;
        var sbClient = provider.GetRequiredService<ServiceBusClient>();
        var sbSettings = provider.GetRequiredService<ServiceBusSettings>();
        _fileUploadFetcher = new NotificationFetcher<BlobCreatedEvent>(
            sbClient, sbSettings.DonorImportFileUploadsTopic, sbSettings.AuditSubscription,
            _pollyRetry, _retry.FetchMessages, "Fetch file upload event");
        _importResultFetcher = new NotificationFetcher<DonorImportMessage>(
            sbClient, sbSettings.DonorImportResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch import result");
        _searchableDonorUpdateFetcher = new NotificationFetcher<SearchableDonorUpdate>(
            sbClient, sbSettings.UpdatedSearchableDonorsTopic, sbSettings.AuditSubscription,
            _pollyRetry, _retry.FetchMessages, "Fetch searchable donor updates");
        _alertFetcher = new NotificationFetcher<Alert>(
            sbClient, sbSettings.AlertsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch alert");
    }

    public async Task<bool> IsFullModeImportAllowed()
    {
        var appName = _azureResource.FunctionApps["DonorImport"];
        var value = await _functionAppHelper.GetAppSetting(appName, "DonorImport:AllowFullModeImport");
        return bool.TryParse(value, out var result) && result;
    }

    public async Task ImportDonorFile(DonorImportRequest request)
    {
        request.UploadedAt = DateTimeOffset.UtcNow;
        await _pollyRetry.ExecuteWithRetry(
            async () => await _blobHelper.UploadDonorFile(request.FileContents, request.FileName),
            _retry.ApiCall, $"Import donor file '{request.FileName}'");
    }

    public async Task<BlobCreatedEvent?> FetchFileUploadEvent(DonorImportRequest request)
    {
        return await _fileUploadFetcher.FetchNotification(
            m => m.Subject.EndsWith(request.FileName),
            $"file upload event for file '{request.FileName}'",
            request.UploadedAt);
    }

    public async Task<DonorImportMessage?> FetchResultMessage(string fileName)
    {
        return await _importResultFetcher.FetchNotification(
            m => m.FileName.EndsWith(fileName),
            $"import result for file '{fileName}'");
    }

    public async Task<IReadOnlyCollection<SearchableDonorUpdate>?> FetchSearchableDonorUpdates(
        IReadOnlyCollection<int> donorIds, DateTimeOffset publishedSince)
    {
        return await _searchableDonorUpdateFetcher.FetchNotifications(
            m => donorIds.Contains(m.DonorId) && m.PublishedDateTime >= publishedSince,
            donorIds.Count,
            $"searchable donor updates for donor ids [{string.Join(", ", donorIds)}] published since {publishedSince:O}",
            publishedSince);
    }

    public async Task<DonorImportHistoryRecord?> FetchFinishedDonorImportHistory(string fileName)
    {
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var record = await _donorSqlHelper.GetDonorImportHistoryByFileName(fileName);
            return record?.FileState == DonorImportHistoryRecord.StartedState ? null : record;
        }, _retry.CheckDonors, $"Fetch finished donor import history for file '{fileName}'");
    }

    public async Task<IReadOnlyCollection<DonorImportFailure>> GetDonorImportFailures(string fileName)
    {
        return await _donorSqlHelper.GetDonorImportFailuresByFileName(fileName);
    }

    public async Task<IReadOnlyCollection<DonorLog>?> FetchDonorLogs(
        IReadOnlyCollection<string> externalDonorCodes, int expectedCount)
    {
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var logs = await _donorSqlHelper.GetDonorLogs(externalDonorCodes);
            return logs.Count == expectedCount ? logs : null;
        }, _retry.CheckDonors, $"Fetch {expectedCount} donor log(s) for codes [{string.Join(", ", externalDonorCodes)}]");
    }

    public async Task<IReadOnlyCollection<PublishableDonorUpdate>?> FetchPublishedDonorUpdates(
        IReadOnlyCollection<int> donorIds, int expectedUpdatesPerDonor)
    {
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var updates = await _donorSqlHelper.GetPublishableDonorUpdates(donorIds);
            var allPublished = donorIds.All(id => updates.Count(u => u.DonorId == id && u.IsPublished) == expectedUpdatesPerDonor);
            return allPublished ? updates : null;
        }, _retry.CheckDonors, $"Fetch {expectedUpdatesPerDonor} published donor update(s) per donor for ids [{string.Join(", ", donorIds)}]");
    }

    public async Task<IReadOnlyCollection<DonorManagementLog>?> FetchDonorManagementLogs(
        IReadOnlyCollection<int> donorIds, DateTimeOffset updatedSince)
    {
        // Atlas bulk-inserts logs for new donors via an untyped DataTable, so LastUpdateDateTime goes through
        // DateTimeOffset.ToString() and loses its fractional seconds; compare at whole-second precision.
        var since = updatedSince.AddTicks(-(updatedSince.Ticks % TimeSpan.TicksPerSecond));
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var logs = await _donorSqlHelper.GetDonorManagementLogs(donorIds);
            var allUpdated = donorIds.All(id => logs.Any(l => l.DonorId == id && l.LastUpdateDateTime >= since));
            if (!allUpdated)
            {
                _logger.LogWarning("Donor management logs not yet updated since {Since:O}: [{Logs}]",
                    since, string.Join(", ", logs.Select(l => $"{l.DonorId}@{l.LastUpdateDateTime:O}")));
                return null;
            }
            return logs;
        }, _retry.CheckDonorsInMatching, $"Fetch donor management logs for ids [{string.Join(", ", donorIds)}] updated since {updatedSince:O}");
    }

    public async Task<DonorCheckResult<Donor>?> CheckDonorsInDonorStore(
        IEnumerable<string> externalDonorCodes, int expectedCount)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var result = await _donorSqlHelper.CheckDonorsInDonorStore(codes);
            if (result.PresentCount != expectedCount)
            {
                _logger.LogWarning("Donor store: found {PresentCount}/{ExpectedCount} donors, absent: [{AbsentDonors}]",
                    result.PresentCount, expectedCount, string.Join(", ", result.AbsentDonors));
                return null;
            }
            return result;
        }, _retry.CheckDonors, $"Check donor store for codes [{codeList}]");
    }

    public async Task<DonorCheckResult<MatchingAlgorithmDonor>?> CheckDonorsAreAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        return await CheckDonorsInMatchingWithCondition(
            codes, result => result.AbsentCount == 0,
            $"Check donors [{codeList}] are available for search");
    }

    public async Task<DonorCheckResult<MatchingAlgorithmDonor>?> CheckDonorsAreNotAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        return await CheckDonorsInMatchingWithCondition(
            codes, result => result.PresentCount == 0,
            $"Check donors [{codeList}] are NOT available for search");
    }

    public async Task<DonorCheckResult<MatchingAlgorithmDonor>?> CheckDonorInfoInMatchingAlgorithmIsAsExpected(IEnumerable<DonorUpdate> donorUpdates)
    {
        var expected = donorUpdates.ToList();
        var codes = expected.GetExternalDonorCodes().ToList();
        var codeList = string.Join(", ", codes);
        return await CheckDonorsInMatchingWithCondition(
            codes,
            result => DonorsMatchExpected(result.PresentDonors, expected),
            $"Check donor info in matching algorithm for [{codeList}]");
    }

    public async Task<Alert?> FetchFailedFileAlert(string fileName)
    {
        return await _alertFetcher.FetchNotification(
            m => m.Description.EndsWith(fileName),
            $"file failure alert for '{fileName}'");
    }

    public async Task<Alert?> FetchHlaExpansionFailureAlert()
    {
        return await _alertFetcher.FetchNotification(
            m => m.Summary.StartsWith("HLA Expansion Failure"),
            "HLA expansion failure alert");
    }

    public async Task<List<HlaExpansionFailure>?> FetchHlaExpansionFailuresForDonor(string externalDonorCode)
    {
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var allFailures = await _appInsightsHelper.GetHlaExpansionFailures(daysToQuery: 1);
            var donorFailures = allFailures
                .Where(f => f.ExternalDonorCodes.Contains(externalDonorCode))
                .ToList();
            return donorFailures.Count > 0 ? donorFailures : null;
        }, _retry.FetchMessages, $"Fetch HLA expansion failures for donor '{externalDonorCode}'");
    }

    public async Task<IReadOnlyCollection<DonorImportFailure>?> FetchDonorImportFailures(string fileName)
    {
        return await _pollyRetry.ExecuteWithRetry(
            async () =>
            {
                var failures = await _donorSqlHelper.GetDonorImportFailuresByFileName(fileName);
                return failures.Count > 0 ? failures : null;
            },
            _retry.FetchMessages, $"Fetch donor import failures for file '{fileName}'");
    }

    private async Task<DonorCheckResult<MatchingAlgorithmDonor>?> CheckDonorsInMatchingWithCondition(
        IEnumerable<string> externalDonorCodes,
        Func<DonorCheckResult<MatchingAlgorithmDonor>, bool> resultIsAsExpected,
        string operationName)
    {
        var codes = externalDonorCodes.ToList();
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var result = await _donorSqlHelper.CheckDonorsInMatchingAlgorithm(codes);
            if (!resultIsAsExpected(result))
            {
                _logger.LogWarning("Matching algorithm: {PresentCount} present, {AbsentCount} absent out of {Total} requested",
                    result.PresentCount, result.AbsentCount, codes.Count);
                return null;
            }
            return result;
        }, _retry.CheckDonorsInMatching, operationName);
    }

    private static bool DonorsMatchExpected(
        IReadOnlyCollection<MatchingAlgorithmDonor> actual,
        IReadOnlyCollection<DonorUpdate> expected)
    {
        if (actual.Count != expected.Count) return false;

        var actualOrdered = actual.OrderBy(d => d.ExternalDonorCode).ToList();
        var expectedOrdered = expected.OrderBy(d => d.RecordId).ToList();

        return actualOrdered.Zip(expectedOrdered).All(pair =>
        {
            var (a, e) = pair;
            return a.ExternalDonorCode == e.RecordId
                   && a.DonorTypeName == e.DonorType.ToString()
                   && a.RegistryCode == e.RegistryCode
                   && a.EthnicityCode == e.Ethnicity
                   && Equals(a.GetHla().ToPhenotypeInfo(), e.Hla?.ToPhenotypeInfoTransfer()?.ToPhenotypeInfo());
        });
    }
}
