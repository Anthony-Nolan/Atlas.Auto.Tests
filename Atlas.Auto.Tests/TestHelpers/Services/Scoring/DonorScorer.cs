using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.Auto.Tests.TestHelpers.Services.Scoring;

internal interface IDonorScorer
{
    Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(BatchScoringRequest request);
}

internal class DonorScorer : IDonorScorer
{
    private readonly IDebugRequester debugRequester;
    private readonly IMatchingAlgorithmFunctionsClient matchingClient;

    public DonorScorer(
        IDebugRequester debugRequester, 
        IMatchingAlgorithmFunctionsClient matchingClient)
    {
        this.debugRequester = debugRequester;
        this.matchingClient = matchingClient;
    }

    public async Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(BatchScoringRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, async () => await Score(request));
    }

    private async Task<DebugResponse<IEnumerable<DonorScoringResult>>> Score(BatchScoringRequest request)
    {
        var result = await matchingClient.ScoreBatch(request);
        return new DebugResponse<IEnumerable<DonorScoringResult>>(result);
    }
}