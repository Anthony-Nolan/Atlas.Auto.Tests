using Atlas.Client.Models.Search.Results.Matching;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search
{
    internal static class MatchingResultNotificationAssertions
    {
        public static void MatchingShouldHaveBeenSuccessful(this MatchingResultsNotification? notification)
        {
            notification.Should().NotBeNull();

            using (new AssertionScope())
            {
                notification!.WasSuccessful.Should().BeTrue();
                notification.FailureInfo.Should().BeNull();
            }
        }

        public static void MatchingShouldHaveFailedValidation(
            this MatchingResultsNotification? notification, string expectedValidationError)
        {
            notification.MatchingShouldHaveFailed();
            notification!.FailureInfo!.ValidationError.Should().Be(expectedValidationError);
        }

        public static void MatchingShouldHaveFailed(this MatchingResultsNotification? notification)
        {
            notification.Should().NotBeNull();

            using (new AssertionScope())
            {
                notification!.WasSuccessful.Should().BeFalse();
                notification.FailureInfo.Should().NotBeNull();
            }
        }
    }
}
