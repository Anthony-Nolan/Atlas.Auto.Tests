using Atlas.Client.Models.Search.Results;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search
{
    internal static class SearchResultsNotificationAssertions
    {
        public static void SearchShouldHaveBeenSuccessful(this SearchResultsNotification? notification)
        {
            notification.Should().NotBeNull();

            using (new AssertionScope())
            {
                notification!.WasSuccessful.Should().BeTrue();
                notification.FailureInfo.Should().BeNull();
            }
        }
    }
}
