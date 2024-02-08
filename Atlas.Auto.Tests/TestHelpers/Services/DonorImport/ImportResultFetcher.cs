using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IImportResultFetcher
{
    Task<DebugResponse<DonorImportMessage>> FetchResultMessage(string fileName);
}

internal class ImportResultFetcher : IImportResultFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IDonorImportFunctionsClient donorImportClient;
    private readonly IMessageFetcher messageFetcher;

    public ImportResultFetcher(
        IDebugRequester debugRequester, 
        IDonorImportFunctionsClient donorImportClient, 
        IMessageFetcher messageFetcher)
    {
        this.debugRequester = debugRequester;
        this.donorImportClient = donorImportClient;
        this.messageFetcher = messageFetcher;
    }

    public async Task<DebugResponse<DonorImportMessage>> FetchResultMessage(string fileName)
    {
        var result = await debugRequester.ExecuteDebugRequestWithWaitAndRetry<DonorImportMessage>(
            10, 20, async () => await FetchMessage(fileName));

        return result;
    }

    private async Task<DebugResponse<DonorImportMessage>> FetchMessage(string fileName)
    {
        var messages = await messageFetcher.FetchAllMessages(request => donorImportClient.PeekDonorImportResultMessages(request));

        // important to use `EndsWith`, as the filename maybe prefixed with blob container path
        var message = messages.LastOrDefault(m => m.FileName.EndsWith(fileName));
            
        return new DebugResponse<DonorImportMessage>(message);
    }
}