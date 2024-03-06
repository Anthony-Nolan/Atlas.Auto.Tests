using Atlas.Client.Models.Search.Results;
using Atlas.Debug.Client.Models.SearchResults;

namespace Atlas.Auto.Tests.TestHelpers.Extensions
{
    internal static class ResultsNotificationExtensions
    {
        public static DebugSearchResultsRequest ToDebugSearchResultsRequest(this ResultsNotification notification)
        {
            return new DebugSearchResultsRequest
            {
                SearchResultBlobContainer = notification.BlobStorageContainerName,
                SearchResultFileName = notification.ResultsFileName,
                BatchFolderName = notification.BatchFolderName
            };
        }
    }
}
