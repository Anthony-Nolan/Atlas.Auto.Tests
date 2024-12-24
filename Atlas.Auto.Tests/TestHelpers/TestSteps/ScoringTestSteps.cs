using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.Client.Models.Scoring.Requests;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

/// <summary>
/// Steps for testing Atlas scoring.
/// Covers arrangement and execution of the scoring workflow and assertion of its outcomes.
/// </summary>
internal interface IScoringTestSteps
{
    Task DonorBatchShouldBeScored(string scoringRequestFileName);

    Task DonorShouldBeScored(string scoringRequestFileName);
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
        var scoreRequest = await SourceDataReader.ReadJsonFile<DonorHlaBatchScoringRequest>(scoringRequestFileName);

        var scoreResponse = await workflow.ScoreBatch(scoreRequest);
        logger.AssertResponseThenLogAndThrow(scoreResponse, "Score request");

        var scoringResult = scoreResponse.DebugResult!.SerializeCollection();
        await logger.AssertThenLogAndThrowAsync(
            () => VerifyJson(scoringResult)
                .WriteReceivedToApprovalsFolder(testName)
                .IgnoreVaryingSearchResultProperties(),
            "Comparison of batch scoring result against approved result");
    }

    public async Task DonorShouldBeScored(string scoringRequestFileName)
    {
        var scoreRequest = await SourceDataReader.ReadJsonFile<DonorHlaScoringRequest>(scoringRequestFileName);

        var scoreResponse = await workflow.Score(scoreRequest);
        logger.AssertResponseThenLogAndThrow(scoreResponse, "Score request");

        var scoringResult = scoreResponse.DebugResult!.SerializeSingle();
        await logger.AssertThenLogAndThrowAsync(
            () => VerifyJson(scoringResult)
                .WriteReceivedToApprovalsFolder(testName)
                .IgnoreVaryingSearchResultProperties(),
            "Comparison of scoring result against approved result");
    }
}