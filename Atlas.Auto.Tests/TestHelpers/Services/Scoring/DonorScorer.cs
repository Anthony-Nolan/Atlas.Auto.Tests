using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Scoring.Requests;
using Atlas.Client.Models.Scoring.Results;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.Scoring;

internal interface IDonorScorer
{
    Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(DonorHlaBatchScoringRequest request);

    Task<DebugResponse<ScoringResult>> Score(DonorHlaScoringRequest request);
}

internal class DonorScorer : IDonorScorer
{
    private readonly IDebugRequester debugRequester;
    private readonly IPublicApiFunctionsClient publicApiClient;

    public DonorScorer(
        IDebugRequester debugRequester,
        IPublicApiFunctionsClient publicApiClient)
    {
        this.debugRequester = debugRequester;
        this.publicApiClient = publicApiClient;
    }

    public async Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(DonorHlaBatchScoringRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, async () => await PostScoreBatch(request));
    }

    public async Task<DebugResponse<ScoringResult>> Score(DonorHlaScoringRequest request)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, async () => await PostScore(request));
    }

    private async Task<DebugResponse<IEnumerable<DonorScoringResult>>> PostScoreBatch(DonorHlaBatchScoringRequest request)
    {
        var result = await publicApiClient.PostScoreBatch(request);
        return new DebugResponse<IEnumerable<DonorScoringResult>>(result);
    }

    private async Task<DebugResponse<ScoringResult>> PostScore(DonorHlaScoringRequest request)
    {
        var result = await publicApiClient.PostScore(request);
        return new DebugResponse<ScoringResult>(result);
    }
}