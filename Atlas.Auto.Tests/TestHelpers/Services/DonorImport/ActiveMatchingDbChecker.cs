using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IActiveMatchingDbChecker
{
    /// <summary>
    /// Use when checking the outcome of donor creation/update.
    /// </summary>
    Task<DebugResponse<DebugDonorsResult>> CheckAllDonorsArePresent(IEnumerable<string> externalDonorCodes);

    /// <summary>
    /// Use when checking the outcome of donor deletion.
    /// todo Atlas-#1221: success of this method depends on completion of https://github.com/Anthony-Nolan/Atlas/issues/1221
    /// </summary>
    Task<DebugResponse<DebugDonorsResult>> CheckAllDonorsAreAbsent(IEnumerable<string> externalDonorCodes);
}

internal class ActiveMatchingDbChecker : IActiveMatchingDbChecker
{
    private const int MaxRetries = 10;
    private const int RetryIntervalSeconds = 20;

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
    public async Task<DebugResponse<DebugDonorsResult>> CheckAllDonorsArePresent(IEnumerable<string> externalDonorCodes)
    {
        return await ExecuteDebugRequestWithWaitAndRetry(
            externalDonorCodes,
            result => result.DonorCounts.Absent == 0);
    }

    /// <inheritdoc />
    public async Task<DebugResponse<DebugDonorsResult>> CheckAllDonorsAreAbsent(IEnumerable<string> externalDonorCodes)
    {
        return await ExecuteDebugRequestWithWaitAndRetry(
            externalDonorCodes,
            result => result.DonorCounts.Present == 0);
    }

    private async Task<DebugResponse<DebugDonorsResult>> ExecuteDebugRequestWithWaitAndRetry(
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