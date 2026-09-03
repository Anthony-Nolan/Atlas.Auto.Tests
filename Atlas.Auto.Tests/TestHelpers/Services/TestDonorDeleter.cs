using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Debug.Client.Clients;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class TestDonorDeleter
{
    private readonly IDonorImportFunctionsClient _donorImportClient;
    private readonly IMatchingAlgorithmFunctionsClient _matchingClient;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly ILogger<TestDonorDeleter> _logger;

    public TestDonorDeleter(
        IDonorImportFunctionsClient donorImportClient,
        IMatchingAlgorithmFunctionsClient matchingClient,
        PollyRetry pollyRetry,
        RetrySettings retry,
        ILogger<TestDonorDeleter> logger)
    {
        _donorImportClient = donorImportClient;
        _matchingClient = matchingClient;
        _pollyRetry = pollyRetry;
        _retry = retry;
        _logger = logger;
    }

    public async Task DeleteDonors()
    {
        var donorCodes = await GetAutoTestDonorCodes();

        if (donorCodes.Count == 0)
        {
            _logger.LogInformation("No donor codes were returned for deletion");
            return;
        }

        var deleteResult = await TryDeleteFromDonorStore(donorCodes);
        _logger.LogInformation("Donors deletion from donor store {Outcome}", deleteResult ? "was successful" : "failed");

        var availabilityResult = await TrySetAsUnavailable(donorCodes);
        _logger.LogInformation("Setting donors as unavailable for search {Outcome}", availabilityResult ? "was successful" : "failed");
    }

    private async Task<IReadOnlyCollection<string>> GetAutoTestDonorCodes()
    {
        var updatedBeforeDate = DateTime.UtcNow.ToString("yyyyMMdd");
        try
        {
            var result = await _pollyRetry.ExecuteWithRetry(
                async () => await _donorImportClient.GetExternalDonorCodesByRegistry(
                    TestConstants.DefaultRegistryCode, updatedBeforeDate),
                _retry.Cleanup, "Fetch auto-test donor codes for cleanup");
            return result?.ToList() ?? new List<string>();
        }
        catch
        {
            _logger.LogWarning("Failed to fetch donor codes for deletion");
            return new List<string>();
        }
    }

    private async Task<bool> TryDeleteFromDonorStore(IReadOnlyCollection<string> donorCodes)
    {
        try
        {
            await _pollyRetry.ExecuteWithRetry(
                async () => await _donorImportClient.DeleteDonors(donorCodes),
                _retry.Cleanup, $"Delete {donorCodes.Count} donors from donor store");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TrySetAsUnavailable(IReadOnlyCollection<string> donorCodes)
    {
        try
        {
            await _pollyRetry.ExecuteWithRetry(
                async () => await _matchingClient.SetDonorsAsUnavailableForSearch(donorCodes),
                _retry.Cleanup, $"Set {donorCodes.Count} donors as unavailable for search");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
