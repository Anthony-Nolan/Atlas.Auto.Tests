using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IDonorCodeFetcher
{
    /// <summary>
    /// Fetches donor codes for auto test donors only.
    /// Can be optionally filtered to only return those donor updated before <paramref name="updatedBeforeDate"/>.
    /// </summary>
    /// <param name="updatedBeforeDate">Must be in `yyyyMMdd` format</param>
    Task<DebugResponse<IEnumerable<string>>> FetchAutoTestDonorCodes(string? updatedBeforeDate = null);
}

internal class DonorCodeFetcher : IDonorCodeFetcher
{
    private readonly IDebugRequester debugRequester;
    private readonly IDonorImportFunctionsClient donorImportClient;

    public DonorCodeFetcher(
        IDebugRequester debugRequester, 
        IDonorImportFunctionsClient donorImportClient)
    {
        this.debugRequester = debugRequester;
        this.donorImportClient = donorImportClient;
    }

    public async Task<DebugResponse<IEnumerable<string>>> FetchAutoTestDonorCodes(string? updatedBeforeDate = null)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            3, 10, async() => await FetchDonorCodes(updatedBeforeDate));
    }

    private async Task<DebugResponse<IEnumerable<string>>> FetchDonorCodes(string? updatedBeforeDate = null)
    {
        var donorsResult = await donorImportClient.GetExternalDonorCodesByRegistry(TestConstants.DefaultRegistryCode, updatedBeforeDate);
        return new DebugResponse<IEnumerable<string>>(donorsResult);
    }
}