using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IDonorImportFailureInfoFetcher
{
    Task<DebugResponse<DonorImportFailureInfo>> FetchFailureInfo(string fileName);
}

internal class DonorImportFailureInfoFetcher : IDonorImportFailureInfoFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IDonorImportFunctionsClient donorImportClient;

    public DonorImportFailureInfoFetcher(
        IDebugRequester debugRequester, 
        IDonorImportFunctionsClient donorImportClient)
    {
        this.debugRequester = debugRequester;
        this.donorImportClient = donorImportClient;
    }

    public async Task<DebugResponse<DonorImportFailureInfo>> FetchFailureInfo(string fileName)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, async () => await FetchInfo(fileName));
    }

    private async Task<DebugResponse<DonorImportFailureInfo>> FetchInfo(string fileName)
    {
        var failureInfo = await donorImportClient.GetDonorImportFailuresByFileName(fileName);

        return new DebugResponse<DonorImportFailureInfo>(failureInfo);
    }
}