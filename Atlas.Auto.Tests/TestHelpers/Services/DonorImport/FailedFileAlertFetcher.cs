using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Client.Models.SupportMessages;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IFailedFileAlertFetcher
{
    Task<DebugResponse<Alert>> FetchAlertMessage(string fileName);
}

internal class FailedFileAlertFetcher : IFailedFileAlertFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly ITopLevelFunctionsClient topLevelClient;
    private readonly IMessageFetcher messageFetcher;

    public FailedFileAlertFetcher(
        IDebugRequester debugRequester, 
        ITopLevelFunctionsClient topLevelClient, 
        IMessageFetcher messageFetcher)
    {
        this.debugRequester = debugRequester;
        this.topLevelClient = topLevelClient;
        this.messageFetcher = messageFetcher;
    }

    public async Task<DebugResponse<Alert>> FetchAlertMessage(string fileName)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, async () => await FetchAlert(fileName));
    }

    private async Task<DebugResponse<Alert>> FetchAlert(string fileName)
    {
        var messages = await messageFetcher.FetchAllMessages(request => topLevelClient.PeekAlerts(request));

        // important to use `EndsWith`, as the filename maybe prefixed with blob container path
        var message = messages.LastOrDefault(m => m.Description.EndsWith(fileName));
            
        return new DebugResponse<Alert>(message);
    }
}