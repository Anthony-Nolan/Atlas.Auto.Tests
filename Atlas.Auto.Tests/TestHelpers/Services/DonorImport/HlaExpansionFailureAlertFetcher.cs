using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.SupportMessages;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IHlaExpansionFailureAlertFetcher
{
    Task<DebugResponse<Alert>> FetchAlertMessage();
}

internal class HlaExpansionFailureAlertFetcher : IHlaExpansionFailureAlertFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly ITopLevelFunctionsClient topLevelClient;
    private readonly IMessageFetcher messageFetcher;

    public HlaExpansionFailureAlertFetcher(
        IDebugRequester debugRequester, 
        ITopLevelFunctionsClient topLevelClient, 
        IMessageFetcher messageFetcher)
    {
        this.debugRequester = debugRequester;
        this.topLevelClient = topLevelClient;
        this.messageFetcher = messageFetcher;
    }

    public async Task<DebugResponse<Alert>> FetchAlertMessage()
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, FetchAlert);
    }

    private async Task<DebugResponse<Alert>> FetchAlert()
    {
        var messages = await messageFetcher.FetchAllMessages(request => topLevelClient.PeekAlerts(request));
        var message = messages.LastOrDefault(m => m.Summary.StartsWith("HLA Expansion Failure"));
        return new DebugResponse<Alert>(message);
    }
}