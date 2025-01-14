using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IActiveMatchingDbChecker
{
    /// <summary>
    /// Use when checking the outcome of donor creation/update.
    /// </summary>
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreAvailableForSearch(IEnumerable<string> externalDonorCodes);

    /// <summary>
    /// Use when checking the outcome of donor deletion.
    /// </summary>
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreNotAvailableForSearch(IEnumerable<string> externalDonorCodes);

    /// <summary>
    /// Use when checking outcome of donor edits that did not involve changing search availability.
    /// </summary>
    Task<DebugResponse<DebugDonorsResult>> CheckDonorInfoIsAsExpected(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo);
}

internal class ActiveMatchingDbChecker : IActiveMatchingDbChecker
{
    private const int MaxRetries = 15;
    private const int RetryIntervalSeconds = 45;

    private readonly IDebugRequester debugRequester;
    private readonly IMatchingAlgorithmFunctionsClient matchingClient;

    public ActiveMatchingDbChecker(
        IDebugRequester debugRequester,
        IMatchingAlgorithmFunctionsClient matchingClient)
    {
        this.debugRequester = debugRequester;
        this.matchingClient = matchingClient;
    }

    /// <inheritdoc />
    public async Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        return await CheckDonorsWithWaitAndRetry(
            externalDonorCodes,
            result => result.DonorCounts.Absent == 0);
    }

    /// <inheritdoc />
    public async Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreNotAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        return await CheckDonorsWithWaitAndRetry(
            externalDonorCodes,
            result => result.DonorCounts.Present == 0);
    }

    public async Task<DebugResponse<DebugDonorsResult>> CheckDonorInfoIsAsExpected(IReadOnlyCollection<DonorDebugInfo> expectedDonorInfo)
    {
        return await CheckDonorsWithWaitAndRetry(
            expectedDonorInfo.GetExternalDonorCodes(),
            result =>
            {
                // must order else comparison will fail even if the same elements are present
                return result.PresentDonors
                    .OrderBy(d => d.ExternalDonorCode)
                    .SequenceEqual(expectedDonorInfo.OrderBy(d => d.ExternalDonorCode));
            });
    }

    private async Task<DebugResponse<DebugDonorsResult>> CheckDonorsWithWaitAndRetry(
        IEnumerable<string> externalDonorCodes,
        Func<DebugDonorsResult, bool> resultIsAsExpected)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            MaxRetries,
            RetryIntervalSeconds,
            async () => await CheckDonorsAndFailResponseIfResultNotAsExpected(externalDonorCodes, resultIsAsExpected));
    }

    /// <summary>
    /// Need to give matching algorithm time to catch up with donor updates.
    /// Therefore, if <paramref name="resultIsAsExpected"/> is not true, will retry the check by failing the response.
    /// This will keep happening until <see cref="MaxRetries"/> is reached or <paramref name="resultIsAsExpected"/> is met.
    /// </summary>
    private async Task<DebugResponse<DebugDonorsResult>> CheckDonorsAndFailResponseIfResultNotAsExpected(
        IEnumerable<string> externalDonorCodes, Func<DebugDonorsResult, bool> resultIsAsExpected)
    {
        var donorsResult = await matchingClient.CheckDonors(externalDonorCodes);
        return new DebugResponse<DebugDonorsResult>(donorsResult != null && resultIsAsExpected(donorsResult), donorsResult);
    }
}