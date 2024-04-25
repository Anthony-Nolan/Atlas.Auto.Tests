using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.MatchingAlgorithm.Client.Models.Scoring;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

/// <summary>
/// Steps for testing Atlas scoring.
/// Covers arrangement and execution of the scoring workflow and assertion of its outcomes.
/// </summary>
internal interface IScoringTestSteps
{
    Task DonorBatchShouldBeScored(string scoringRequestFileName);
}

internal class ScoringTestSteps : IScoringTestSteps
{
    private readonly IScoringWorkflow workflow;
    private readonly ITestLogger logger;
    private readonly string testName;

    public ScoringTestSteps(
        IScoringWorkflow workflow,
        ITestLogger logger,
        string testName)
    {
        this.workflow = workflow;
        this.logger = logger;
        this.testName = testName;
    }

    public async Task DonorBatchShouldBeScored(string scoringRequestFileName)
    {
        var scoreRequest = await SourceDataReader.ReadJsonFile<BatchScoringRequest>(scoringRequestFileName);

        var scoreResponse = await workflow.ScoreBatch(scoreRequest);
        logger.AssertResponseThenLogAndThrow(scoreResponse, "Score request");

        var scoringResult = scoreResponse.DebugResult!.SerializeCollection();
        await logger.AssertThenLogAndThrowAsync(
            () => VerifyJson(scoringResult)
                .WriteReceivedToApprovalsFolder(testName)
                .IgnoreVaryingSearchResultProperties(),
            "Comparison of scoring result against approved result");
    }
}