using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.Scoring;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

/// <summary>
/// Workflow to execute a scoring request.
/// </summary>
internal interface IScoringWorkflow
{
    Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(BatchScoringRequest request);
}

internal class ScoringWorkflow : IScoringWorkflow
{
    private readonly IDonorScorer scorer;

    public ScoringWorkflow(IDonorScorer scorer)
    {
        this.scorer = scorer;
    }

    public async Task<DebugResponse<IEnumerable<DonorScoringResult>>> ScoreBatch(BatchScoringRequest request)
    {
        return await scorer.ScoreBatch(request);
    }
}