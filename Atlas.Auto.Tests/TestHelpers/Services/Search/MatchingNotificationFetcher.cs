using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.Search;

internal interface IMatchingNotificationFetcher
{
    Task<DebugResponse<MatchingResultsNotification>> FetchNotification(string searchRequestId);
}

internal class MatchingNotificationFetcher : IMatchingNotificationFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IMatchingAlgorithmFunctionsClient matchingAlgorithmClient;
    private readonly IMessageFetcher messageFetcher;

    public MatchingNotificationFetcher(
        IDebugRequester debugRequester,
        IMatchingAlgorithmFunctionsClient matchingAlgorithmClient,
        IMessageFetcher messageFetcher)
    {
        this.debugRequester = debugRequester;
        this.matchingAlgorithmClient = matchingAlgorithmClient;
        this.messageFetcher = messageFetcher;
    }

    public async Task<DebugResponse<MatchingResultsNotification>> FetchNotification(string searchRequestId)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry<MatchingResultsNotification>(
            45, 20, async () => await FetchMessage(searchRequestId));
    }

    private async Task<DebugResponse<MatchingResultsNotification>> FetchMessage(string searchRequestId)
    {
        var messages = await messageFetcher.FetchAllMessages(request => matchingAlgorithmClient.PeekMatchingResultNotifications(request));
        var message = messages.LastOrDefault(m => m.SearchRequestId == searchRequestId);
        return new DebugResponse<MatchingResultsNotification>(message);
    }
}
