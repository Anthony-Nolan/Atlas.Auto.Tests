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
    private readonly NotificationFetcher<DonorImportMessage> _importResultFetcher;
    private readonly NotificationFetcher<Alert> _alertFetcher;

    public DonorImportWorkflow(IServiceProvider provider)
    {
        _functionAppHelper = provider.GetRequiredService<FunctionAppHelper>();
        _azureResource = provider.GetRequiredService<AzureResourceSettings>();
        _appInsightsHelper = provider.GetRequiredService<AppInsightsHelper>();
        _donorSqlHelper = provider.GetRequiredService<IDonorStoreSqlHelper>();
        _blobHelper = provider.GetRequiredService<BlobStorageHelper>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        var sbClient = provider.GetRequiredService<ServiceBusClient>();
        var sbSettings = provider.GetRequiredService<ServiceBusSettings>();
        _importResultFetcher = new NotificationFetcher<DonorImportMessage>(
            sbClient, sbSettings.DonorImportResultsTopic, sbSettings.Subscription,
            _pollyRetry, _retry.FetchMessages, "Fetch import result");
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
        await _pollyRetry.ExecuteWithRetry(
            async () => await _blobHelper.UploadDonorFile(request.FileContents, request.FileName),
            _retry.ApiCall, $"Import donor file '{request.FileName}'");
    }

    public async Task<DonorImportMessage?> FetchResultMessage(string fileName)
    {
        return await _importResultFetcher.FetchNotification(
            m => m.FileName.EndsWith(fileName),
            $"import result for file '{fileName}'");
    }

    public async Task<DonorCheckResult<Donor>?> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        return await _pollyRetry.ExecuteWithRetry(
            async () => await _donorSqlHelper.CheckDonorsInDonorStore(codes),
            _retry.CheckDonors, $"Check donor store for codes [{codeList}]");
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
            return resultIsAsExpected(result) ? result : null;
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
