using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search;

internal static class SearchTrackingAssertions
{
    /// <param name="originalSearchId">Null for a new search; the original search ID for a repeat search.</param>
    public static void MatchingShouldHaveFailed(
        this SearchTrackingRequest request, ExpectedMatchingFailure expected, string? originalSearchId)
    {
        using (new AssertionScope())
        {
            // Search or repeat search
            request.IsRepeatSearch.Should().Be(originalSearchId != null);
            request.OriginalSearchIdentifier.Should().Be(originalSearchId?.ToLowerInvariant());

            // Matching failed on its only attempt and found nothing
            request.MatchingAlgorithm_IsSuccessful.Should().BeFalse();
            request.MatchingAlgorithm_FailureInfo_Type.Should().Be(expected.Type);
            request.MatchingAlgorithm_FailureInfo_Message.Should().Be(expected.Message);
            // Not the exact text: it holds build paths and line numbers
            request.MatchingAlgorithm_FailureInfo_ExceptionStacktrace.Should().NotBeNullOrWhiteSpace();
            request.MatchingAlgorithm_TotalAttemptsNumber.Should().Be(1);
            request.MatchingAlgorithm_NumberOfResults.Should().BeNull();
            request.MatchingAlgorithm_HlaNomenclatureVersion.Should().NotBeNullOrWhiteSpace();
            request.MatchingAlgorithm_ResultsSent.Should().BeTrue();
            request.MatchingAlgorithm_ResultsSentTimeUtc.Should().NotBeNull();

            // Match prediction never ran, but the failure notification was sent
            request.MatchPrediction_IsSuccessful.Should().BeNull();
            request.MatchPrediction_FailureInfo_Type.Should().BeNull();
            request.MatchPrediction_FailureInfo_Message.Should().BeNull();
            request.MatchPrediction_FailureInfo_ExceptionStacktrace.Should().BeNull();
            request.MatchPrediction_DonorsPerBatch.Should().BeNull();
            request.MatchPrediction_TotalNumberOfBatches.Should().BeNull();
            request.ResultsSent.Should().BeTrue();
            request.ResultsSentTimeUtc.Should().NotBeNull();
        }
    }

    /// <summary>
    /// The attempt failed before matching started: no matching, scoring or persisting timings.
    /// </summary>
    public static void ShouldHaveFailedBeforeMatching(
        this SearchTrackingMatchingAlgorithmAttempt attempt, ExpectedMatchingFailure expected)
    {
        using (new AssertionScope())
        {
            attempt.AttemptNumber.Should().Be(1);
            attempt.InitiationTimeUtc.Should().NotBe(default);
            attempt.StartTimeUtc.Should().NotBe(default);
            attempt.CompletionTimeUtc.Should().NotBeNull();

            attempt.AlgorithmCore_Matching_StartTimeUtc.Should().BeNull();
            attempt.AlgorithmCore_Matching_EndTimeUtc.Should().BeNull();
            attempt.AlgorithmCore_Scoring_StartTimeUtc.Should().BeNull();
            attempt.AlgorithmCore_Scoring_EndTimeUtc.Should().BeNull();
            attempt.PersistingResults_StartTimeUtc.Should().BeNull();
            attempt.PersistingResults_EndTimeUtc.Should().BeNull();

            attempt.IsSuccessful.Should().BeFalse();
            attempt.FailureInfo_Type.Should().Be(expected.Type);
            attempt.FailureInfo_Message.Should().Be(expected.Message);
            attempt.FailureInfo_ExceptionStacktrace.Should().NotBeNullOrWhiteSpace();
        }
    }
}
