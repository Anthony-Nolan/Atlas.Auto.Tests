using Atlas.Client.Models.Search.Results.Matching;
using FluentAssertions;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search
{
    internal static class MatchingResultNotificationAssertions
    {
        public static void MatchingShouldHaveBeenSuccessful(this MatchingResultsNotification? notification)
        {
            notification.Should().NotBeNull();
            notification!.WasSuccessful.Should().BeTrue(
                "Matching for search {0} (repeat search {1}) should have been successful but got failure: {2}",
                notification.SearchRequestId,
                notification.RepeatSearchRequestId ?? "n/a",
                JsonConvert.SerializeObject(notification.FailureInfo));
            notification.FailureInfo.Should().BeNull();
        }

        public static void MatchingShouldHaveFailedHlaValidation(this MatchingResultsNotification? notification)
        {
            notification.MatchingShouldHaveFailed();
            notification!.FailureInfo.ValidationError.Should().NotBeNull();
            notification.FailureInfo.ValidationError.Should().StartWith("Failed to lookup");
        }

        public static void MatchingShouldHaveFailed(this MatchingResultsNotification? notification)
        {
            notification.Should().NotBeNull();
            notification!.WasSuccessful.Should().BeFalse();
            notification.FailureInfo.Should().NotBeNull();
        }
    }
}
