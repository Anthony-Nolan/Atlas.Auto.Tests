using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

internal interface IDonorStoreChecker
{
    Task<DebugResponse<DebugDonorsResult>> Check(IEnumerable<string> externalDonorCodes);
}

internal class DonorStoreChecker : IDonorStoreChecker
{
    private readonly IDebugRequester debugRequester;
    private readonly IDonorImportFunctionsClient donorImportClient;

    public DonorStoreChecker(
        IDebugRequester debugRequester, 
        IDonorImportFunctionsClient donorImportClient)
    {
        this.debugRequester = debugRequester;
        this.donorImportClient = donorImportClient;
    }

    public async Task<DebugResponse<DebugDonorsResult>> Check(IEnumerable<string> externalDonorCodes)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            10, 20, async () => await CheckDonorStore(externalDonorCodes));
    }

    private async Task<DebugResponse<DebugDonorsResult>> CheckDonorStore(IEnumerable<string> externalDonorCodes)
    {
        var donorsResult = await donorImportClient.CheckDonors(externalDonorCodes);
        return new DebugResponse<DebugDonorsResult>(donorsResult);
    }
}