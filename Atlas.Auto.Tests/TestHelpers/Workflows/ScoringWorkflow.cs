using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.Scoring;
using Atlas.Client.Models.Scoring.Requests;
using Atlas.Client.Models.Scoring.Results;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

/// <summary>
/// Workflow to execute a scoring request.
/// </summary>
internal interface IScoringWorkflow
{
    Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(DonorHlaBatchScoringRequest request);

    Task<DebugResponse<ScoringResult>> Score(DonorHlaScoringRequest request);
}

internal class ScoringWorkflow : IScoringWorkflow
{
    private readonly IDonorScorer scorer;

    public ScoringWorkflow(IDonorScorer scorer)
    {
        this.scorer = scorer;
    }

    public async Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(DonorHlaBatchScoringRequest request)
    {
        return await scorer.ScoreBatch(request);
    }

    public async Task<DebugResponse<ScoringResult>> Score(DonorHlaScoringRequest request)
    {
        return await scorer.Score(request);
    }
}