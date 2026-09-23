using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.Services;

internal class TestDonorDeleter
{
    private readonly IDonorStoreSqlHelper _donorSqlHelper;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;
    private readonly ILogger<TestDonorDeleter> _logger;

    public TestDonorDeleter(IServiceProvider provider)
    {
        _donorSqlHelper = provider.GetRequiredService<IDonorStoreSqlHelper>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
        _logger = provider.GetRequiredService<ILogger<TestDonorDeleter>>();
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
        _logger.LogInformation("Donors deletion from donor store {Outcome:l}", deleteResult ? "was successful" : "failed");

        var availabilityResult = await TrySetAsUnavailable(donorCodes);
        _logger.LogInformation("Setting donors as unavailable for search {Outcome:l}", availabilityResult ? "was successful" : "failed");
    }

    private async Task<IReadOnlyCollection<string>> GetAutoTestDonorCodes()
    {
        var updatedBefore = DateTimeOffset.UtcNow;
        try
        {
            var result = await _pollyRetry.ExecuteWithRetry(
                async () => await _donorSqlHelper.GetExternalDonorCodesByRegistry(
                    TestConstants.DefaultRegistryCode, updatedBefore),
                _retry.Cleanup, "Fetch auto-test donor codes for cleanup");
            return result?.ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch donor codes for deletion");
            return new List<string>();
        }
    }

    private async Task<bool> TryDeleteFromDonorStore(IReadOnlyCollection<string> donorCodes)
    {
        try
        {
            await _pollyRetry.ExecuteWithRetry(
                async () => await _donorSqlHelper.DeleteDonorsFromDonorStore(donorCodes),
                _retry.Cleanup, $"Delete {donorCodes.Count} donors from donor store");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete donors from donor store");
            return false;
        }
    }

    private async Task<bool> TrySetAsUnavailable(IReadOnlyCollection<string> donorCodes)
    {
        try
        {
            await _pollyRetry.ExecuteWithRetry(
                async () => await _donorSqlHelper.SetDonorsAsUnavailableForSearch(donorCodes),
                _retry.Cleanup, $"Set {donorCodes.Count} donors as unavailable for search");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set donors as unavailable for search");
            return false;
        }
    }
}
