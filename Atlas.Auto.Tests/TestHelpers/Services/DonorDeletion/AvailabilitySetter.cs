using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorDeletion;

internal interface IAvailabilitySetter
{
    /// <returns>`true` if request to make donors unavailable for search completed without error, else `false`</returns>
    Task<bool> SetDonorsAsUnavailableForSearch(IEnumerable<string> externalDonorCodes);
}

internal class AvailabilitySetter : IAvailabilitySetter
{
    private readonly IDebugRequester debugRequester;
    private readonly IMatchingAlgorithmFunctionsClient matchingAlgorithmClient;

    public AvailabilitySetter(
        IDebugRequester debugRequester, 
        IMatchingAlgorithmFunctionsClient matchingAlgorithmClient)
    {
        this.debugRequester = debugRequester;
        this.matchingAlgorithmClient = matchingAlgorithmClient;
    }

    public async Task<bool> SetDonorsAsUnavailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            3, 10, async () => await MakeUnavailableForSearch(externalDonorCodes));
    }

    private async Task<bool> MakeUnavailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        await matchingAlgorithmClient.SetDonorsAsUnavailableForSearch(externalDonorCodes);
        return true;
    }
}