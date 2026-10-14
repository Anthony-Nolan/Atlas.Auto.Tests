namespace Atlas.Auto.Tests.Tests.Scoring;

[TestFixture]
[Category(nameof(ScoringHappyPathTests))]
internal class ScoringHappyPathTests : ScoringTestBase
{
    public ScoringHappyPathTests() : base(nameof(ScoringHappyPathTests))
    {
    }

    [Test]
    public async Task ScoreBatch_ScoresDonorBatch()
    {
        var steps = GetScoringTestSteps(nameof(ScoreBatch_ScoresDonorBatch));
        await steps.DonorBatchShouldBeScored("score-batch-request.json");
    }

    [Test]
    public async Task Score_ScoresDonor()
    {
        var steps = GetScoringTestSteps(nameof(Score_ScoresDonor));
        await steps.DonorShouldBeScored("score-request.json");
    }
}
