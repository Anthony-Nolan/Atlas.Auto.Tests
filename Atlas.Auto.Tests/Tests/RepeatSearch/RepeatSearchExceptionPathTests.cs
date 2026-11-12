using Atlas.Auto.Tests.TestHelpers.Assertions.Search;
using Atlas.Auto.Tests.TestHelpers.Attributes;
using Atlas.Auto.Tests.TestHelpers.InternalModels;

namespace Atlas.Auto.Tests.Tests.RepeatSearch;

[TestFixture]
[Category(nameof(RepeatSearchExceptionPathTests))]
internal class RepeatSearchExceptionPathTests : RepeatSearchTestBase
{
    public RepeatSearchExceptionPathTests() : base(nameof(RepeatSearchExceptionPathTests))
    {
    }

    [Test]
    public async Task RepeatSearch_MissingRequiredInfo_ReturnsValidationError()
    {
        var steps = GetRepeatSearchTestSteps(nameof(RepeatSearch_MissingRequiredInfo_ReturnsValidationError));
        await steps.RepeatRequestMissingRequiredInfoShouldReturnValidationErrors();
    }

    /// <summary>
    /// QA-2921: the public API accepts a repeat search of a search Atlas has no record of; matching then fails validation,
    /// and search tracking records the failure: one failed attempt, no match prediction, every event sent and none dead-lettered.
    /// </summary>
    [Test]
    [ManualTest("QA-1980")]
    [AutoTest("QA-2921")]
    public async Task RepeatSearch_UnknownOriginalSearch_FailsValidation()
    {
        var steps = GetRepeatSearchTestSteps(nameof(RepeatSearch_UnknownOriginalSearch_FailsValidation));
        // A new ID, so no search has it
        var originalSearchId = Guid.NewGuid().ToString();
        var expectedFailure = ExpectedMatchingFailure.NoOriginalSearch(originalSearchId);

        // Matching fails validation and says so in its notification
        var repeatSearchId = await steps.SubmitRepeatSearchRequest(
            "search-request-donor-10_10.json", originalSearchId, DateTimeOffset.UtcNow);
        await steps.RepeatMatchingShouldHaveFailedValidation(repeatSearchId, originalSearchId, expectedFailure.Message);

        // Search tracking stores the failure under the repeat search ID, the failed attempt and no match prediction
        var tracking = await steps.SearchTracking.SearchShouldHaveFinished(repeatSearchId);
        tracking.MatchingShouldHaveFailed(expectedFailure, originalSearchId);
        await steps.SearchTracking.OneFailedAttemptShouldHaveBeenStored(tracking, expectedFailure);
        await steps.SearchTracking.MatchPredictionShouldNotHaveBeenStored(tracking);

        // Every event went through search-tracking-events and was processed
        await steps.SearchTracking.EventsShouldHaveBeenSent(tracking, originalSearchId,
        [
            SearchTrackingEventTypes.SearchRequested,
            SearchTrackingEventTypes.MatchingAlgorithmAttemptStarted,
            SearchTrackingEventTypes.MatchingAlgorithmCompleted,
            SearchTrackingEventTypes.MatchPredictionResultsSent
        ]);
        await steps.SearchTracking.EventsShouldNotHaveBeenDeadLettered(tracking);
    }
}
