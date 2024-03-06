using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.RepeatSearch;

internal interface IRepeatSearchNotificationFetcher
{
    Task<DebugResponse<SearchResultsNotification>> FetchNotification(string repeatSearchId, string searchRequestId);
}

internal class RepeatSearchNotificationFetcher : IRepeatSearchNotificationFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly ITopLevelFunctionsClient topLevelClient;
    private readonly IMessageFetcher messageFetcher;

    public RepeatSearchNotificationFetcher(
        IDebugRequester debugRequester, 
        ITopLevelFunctionsClient topLevelClient, 
        IMessageFetcher messageFetcher)
    {
        this.debugRequester = debugRequester;
        this.topLevelClient = topLevelClient;
        this.messageFetcher = messageFetcher;
    }

    public async Task<DebugResponse<SearchResultsNotification>> FetchNotification(string repeatSearchId, string searchRequestId)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            10, 20, async () => await FetchMessage(repeatSearchId, searchRequestId));
    }

    private async Task<DebugResponse<SearchResultsNotification>> FetchMessage(string repeatSearchId, string searchRequestId)
    {
        var messages = await messageFetcher.FetchAllMessages(
            request => topLevelClient.PeekRepeatSearchResultNotifications(request));
        
        var message = messages.LastOrDefault(m => 
            m.RepeatSearchRequestId == repeatSearchId &&
            m.SearchRequestId == searchRequestId);
        
        return new DebugResponse<SearchResultsNotification>(message);
    }
}