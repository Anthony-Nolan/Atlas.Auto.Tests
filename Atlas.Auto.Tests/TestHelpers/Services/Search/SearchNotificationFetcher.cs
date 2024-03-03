using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.Search;

internal interface ISearchNotificationFetcher
{
    Task<DebugResponse<SearchResultsNotification>> FetchNotification(string searchRequestId);
}

internal class SearchNotificationFetcher : ISearchNotificationFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly ITopLevelFunctionsClient topLevelClient;
    private readonly IMessageFetcher messageFetcher;

    public SearchNotificationFetcher(
        IDebugRequester debugRequester, 
        ITopLevelFunctionsClient topLevelClient, 
        IMessageFetcher messageFetcher)
    {
        this.debugRequester = debugRequester;
        this.topLevelClient = topLevelClient;
        this.messageFetcher = messageFetcher;
    }

    public async Task<DebugResponse<SearchResultsNotification>> FetchNotification(string searchRequestId)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, async () => await FetchMessage(searchRequestId));
    }

    private async Task<DebugResponse<SearchResultsNotification>> FetchMessage(string searchRequestId)
    {
        var messages = await messageFetcher.FetchAllMessages(request => topLevelClient.PeekSearchResultNotifications(request));
        var message = messages.LastOrDefault(m => m.SearchRequestId == searchRequestId);
        return new DebugResponse<SearchResultsNotification>(message);
    }
}