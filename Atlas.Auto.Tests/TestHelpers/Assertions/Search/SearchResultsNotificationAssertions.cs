using Atlas.Client.Models.Search.Results;
using FluentAssertions;
using Newtonsoft.Json;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.Search
{
    internal static class SearchResultsNotificationAssertions
    {
        public static void SearchShouldHaveBeenSuccessful(this SearchResultsNotification? notification)
        {
            notification.Should().NotBeNull();
            notification!.WasSuccessful.Should().BeTrue(
                "Search {0} (repeat search {1}) should have been successful but got failure: {2}",
                notification.SearchRequestId,
                notification.RepeatSearchRequestId ?? "n/a",
                JsonConvert.SerializeObject(notification.FailureInfo));
            notification.FailureInfo.Should().BeNull();
        }
    }
}
