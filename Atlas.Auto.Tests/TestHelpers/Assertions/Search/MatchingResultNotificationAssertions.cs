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
    }
}
