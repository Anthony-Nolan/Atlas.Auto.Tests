using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IFullModeChecker
{
    Task<DebugResponse<bool>> IsFullModeImportAllowed();
}

internal class FullModeChecker : IFullModeChecker
{
    private readonly IDebugRequester debugRequester;
    private readonly IDonorImportFunctionsClient donorImportClient;

    public FullModeChecker(
        IDebugRequester debugRequester, 
        IDonorImportFunctionsClient donorImportClient)
    {
        this.debugRequester = debugRequester;
        this.donorImportClient = donorImportClient;
    }

    public async Task<DebugResponse<bool>> IsFullModeImportAllowed()
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(10, 20, CheckIsFullModeAllowed);
    }

    private async Task<DebugResponse<bool>> CheckIsFullModeAllowed()
    {
        var result = await donorImportClient.IsFullModeImportAllowed();
        return new DebugResponse<bool>(result);
    }
}