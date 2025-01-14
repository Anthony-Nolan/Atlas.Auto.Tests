using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.ApplicationInsights;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IHlaExpansionFailureFetcher
{
    Task<DebugResponse<IEnumerable<HlaExpansionFailure>>> FetchHlaExpansionFailuresForDonor(string externalDonorCode);
}

internal class HlaExpansionFailureFetcher : IHlaExpansionFailureFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IMatchingAlgorithmFunctionsClient matchingAlgorithmClient;

    public HlaExpansionFailureFetcher(
        IDebugRequester debugRequester,
        IMatchingAlgorithmFunctionsClient matchingAlgorithmClient)
    {
        this.debugRequester = debugRequester;
        this.matchingAlgorithmClient = matchingAlgorithmClient;
    }

    public async Task<DebugResponse<IEnumerable<HlaExpansionFailure>>> FetchHlaExpansionFailuresForDonor(string externalDonorCode)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            10, 20, () =>FetchExpansionFailures(externalDonorCode));
    }

    private async Task<DebugResponse<IEnumerable<HlaExpansionFailure>>> FetchExpansionFailures(string externalDonorCode)
    {
        var allFailures = await matchingAlgorithmClient.GetHlaExpansionFailures(1);

        var donorFailures =
            allFailures?.Where(f => f.ExternalDonorCodes.Contains(externalDonorCode)).ToList()
            ?? new List<HlaExpansionFailure>();

        return new DebugResponse<IEnumerable<HlaExpansionFailure>>(donorFailures.Any(), donorFailures);
    }
}