using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.RepeatSearch;

internal interface IRepeatMatchingNotificationFetcher
{
    Task<DebugResponse<MatchingResultsNotification>> FetchNotification(string repeatSearchRequestId, string searchRequestId);
}

internal class RepeatMatchingNotificationFetcher : IRepeatMatchingNotificationFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IRepeatSearchFunctionsClient repeatSearchClient;
    private readonly IMessageFetcher messageFetcher;

    public RepeatMatchingNotificationFetcher(
        IDebugRequester debugRequester, 
        IRepeatSearchFunctionsClient repeatSearchClient, 
        IMessageFetcher messageFetcher)
    {
        this.debugRequester = debugRequester;
        this.repeatSearchClient = repeatSearchClient;
        this.messageFetcher = messageFetcher;
    }

    public async Task<DebugResponse<MatchingResultsNotification>> FetchNotification(string repeatSearchRequestId, string searchRequestId)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry<MatchingResultsNotification>(
            10, 20, async () => await FetchMessage(repeatSearchRequestId, searchRequestId));
    }

    private async Task<DebugResponse<MatchingResultsNotification>> FetchMessage(string repeatSearchRequestId, string searchRequestId)
    {
        var messages = await messageFetcher.FetchAllMessages(
            request => repeatSearchClient.PeekMatchingResultNotifications(request));
        
        var message = messages.LastOrDefault(m => 
            m.RepeatSearchRequestId == repeatSearchRequestId && 
            m.SearchRequestId == searchRequestId);

        return new DebugResponse<MatchingResultsNotification>(message);
    }
}