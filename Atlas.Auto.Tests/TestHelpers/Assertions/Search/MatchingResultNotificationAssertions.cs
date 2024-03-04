using Atlas.Client.Models.Search.Results.Matching;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search
{
    internal static class MatchingResultNotificationAssertions
    {
        public static void MatchingShouldHaveBeenSuccessful(this MatchingResultsNotification? notification)
        {
            notification.Should().NotBeNull();
            notification!.WasSuccessful.Should().BeTrue();
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
