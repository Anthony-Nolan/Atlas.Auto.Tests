namespace Atlas.Auto.Tests.Tests.Scoring;

[TestFixture]
[Category($"{nameof(ScoringHappyPathTests)}")]
internal class ScoringHappyPathTests : ScoringTestBase
{
    public ScoringHappyPathTests() : base(nameof(ScoringHappyPathTests))
    {
    }

    [Test]
    public async Task ScoreBatch_ScoresDonorBatch()
    {
        var steps = GetScoringTestSteps(nameof(ScoreBatch_ScoresDonorBatch));
        const string testDescription = "Score donor batch";
        steps.Logger.LogStart(testDescription);
        await steps.DonorBatchShouldBeScored("score-batch-request.json");
        steps.Logger.LogCompletion(testDescription);
    }

    [Test]
    public async Task Score_ScoresDonor()
    {
        var steps = GetScoringTestSteps(nameof(Score_ScoresDonor));
        const string testDescription = "Score donor";
        steps.Logger.LogStart(testDescription);
        await steps.DonorShouldBeScored("score-request.json");
        steps.Logger.LogCompletion(testDescription);
    }
}
