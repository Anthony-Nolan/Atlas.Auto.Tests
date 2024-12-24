using static System.Formats.Asn1.AsnWriter;

namespace Atlas.Auto.Tests.Tests.Scoring;

/// <summary>
/// Tests that cover happy paths of Atlas scoring.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category($"{nameof(Scoring_HappyPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class Scoring_HappyPathTests : ScoringTestBase
{
    public Scoring_HappyPathTests() : base(nameof(Scoring_HappyPathTests))
    {
    }

    [Test]
    public async Task ScoreBatch_ScoresDonorBatch()
    {
        var test = GetTestServices(nameof(ScoreBatch_ScoresDonorBatch));
        const string testDescription = "Score donor batch";
        test.Logger.LogStart(testDescription);
        await test.Steps.DonorBatchShouldBeScored("score-batch-request.json");
        test.Logger.LogCompletion(testDescription);
    }

    [Test]
    public async Task Score_ScoresDonor()
    {
        var test = GetTestServices(nameof(Score_ScoresDonor));
        const string testDescription = "Score donor";
        test.Logger.LogStart(testDescription);
        await test.Steps.DonorShouldBeScored("score-request.json");
        test.Logger.LogCompletion(testDescription);
    }
}