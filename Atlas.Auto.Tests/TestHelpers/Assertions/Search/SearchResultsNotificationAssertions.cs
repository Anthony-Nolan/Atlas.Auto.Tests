using Atlas.Client.Models.Search.Results;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search
{
    internal static class SearchResultsNotificationAssertions
    {
        public static void SearchShouldHaveBeenSuccessful(this SearchResultsNotification? notification)
        {
            notification.Should().NotBeNull();
            notification!.WasSuccessful.Should().BeTrue();
            notification.FailureInfo.Should().BeNull();
        }
    }
}
