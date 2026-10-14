using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.SupportMessages;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.ApplicationInsights;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

internal class DonorImportWorkflow
{
    private readonly IDonorImportFunctionsClient _donorImportClient;
    private readonly IMatchingAlgorithmFunctionsClient _matchingClient;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly NotificationFetcher<DonorImportMessage> _importResultFetcher;
    private readonly NotificationFetcher<Alert> _alertFetcher;

    public DonorImportWorkflow(IServiceProvider provider)
    {
        _donorImportClient = provider.GetRequiredService<IDonorImportFunctionsClient>();
        _matchingClient = provider.GetRequiredService<IMatchingAlgorithmFunctionsClient>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        var topLevelClient = provider.GetRequiredService<ITopLevelFunctionsClient>();
        _importResultFetcher = new NotificationFetcher<DonorImportMessage>(
            req => _donorImportClient.PeekDonorImportResultMessages(req), _pollyRetry, _retry.FetchMessages, "Fetch import result");
        _alertFetcher = new NotificationFetcher<Alert>(
            req => topLevelClient.PeekAlerts(req), _pollyRetry, _retry.FetchMessages, "Fetch alert");
    }

    public async Task<bool> IsFullModeImportAllowed()
    {
        var result = false;
        await _pollyRetry.ExecuteWithRetry(async () =>
        {
            result = await _donorImportClient.IsFullModeImportAllowed();
        }, _retry.ApiCall, "Check if full mode import is allowed");
        return result;
    }

    public async Task ImportDonorFile(DonorImportRequest request)
    {
        await _pollyRetry.ExecuteWithRetry(
            async () => await _donorImportClient.ImportFile(request),
            _retry.ApiCall, $"Import donor file '{request.FileName}'");
    }

    public async Task<DonorImportMessage?> FetchResultMessage(string fileName)
    {
        return await _importResultFetcher.FetchNotification(
            m => m.FileName.EndsWith(fileName),
            $"import result for file '{fileName}'");
    }

    public async Task<DebugDonorsResult?> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        return await _pollyRetry.ExecuteWithRetry(
            async () => await _donorImportClient.CheckDonors(codes),
            _retry.CheckDonors, $"Check donor store for codes [{codeList}]");
    }

    public async Task<DebugDonorsResult?> CheckDonorsAreAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        return await CheckDonorsInMatchingWithCondition(
            codes, result => result.DonorCounts.Absent == 0,
            $"Check donors [{codeList}] are available for search");
    }

    public async Task<DebugDonorsResult?> CheckDonorsAreNotAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        var codes = externalDonorCodes.ToList();
        var codeList = string.Join(", ", codes);
        return await CheckDonorsInMatchingWithCondition(
            codes, result => result.DonorCounts.Present == 0,
            $"Check donors [{codeList}] are NOT available for search");
    }

    public async Task<DebugDonorsResult?> CheckDonorInfoInMatchingAlgorithmIsAsExpected(IEnumerable<DonorDebugInfo> donorInfo)
    {
        var expected = donorInfo.ToList();
        var codes = expected.GetExternalDonorCodes().ToList();
        var codeList = string.Join(", ", codes);
        return await CheckDonorsInMatchingWithCondition(
            codes,
            result => result.PresentDonors
                .OrderBy(d => d.ExternalDonorCode)
                .SequenceEqual(expected.OrderBy(d => d.ExternalDonorCode)),
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
            var allFailures = await _matchingClient.GetHlaExpansionFailures(1);
            var donorFailures = allFailures?
                .Where(f => f.ExternalDonorCodes.Contains(externalDonorCode))
                .ToList();
            return donorFailures is { Count: > 0 } ? donorFailures : null;
        }, _retry.FetchMessages, $"Fetch HLA expansion failures for donor '{externalDonorCode}'");
    }

    public async Task<DonorImportFailureInfo?> FetchDonorImportFailureInfo(string fileName)
    {
        return await _pollyRetry.ExecuteWithRetry(
            async () => await _donorImportClient.GetDonorImportFailuresByFileName(fileName),
            _retry.FetchMessages, $"Fetch donor import failure info for file '{fileName}'");
    }

    private async Task<DebugDonorsResult?> CheckDonorsInMatchingWithCondition(
        IEnumerable<string> externalDonorCodes,
        Func<DebugDonorsResult, bool> resultIsAsExpected,
        string operationName)
    {
        var codes = externalDonorCodes.ToList();
        return await _pollyRetry.ExecuteWithRetry(async () =>
        {
            var result = await _matchingClient.CheckDonors(codes);
            return result != null && resultIsAsExpected(result) ? result : null;
        }, _retry.CheckDonorsInMatching, operationName);
    }
}
