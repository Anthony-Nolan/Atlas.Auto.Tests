using Atlas.Debug.Client.Clients;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorDeletion;

internal interface IDonorDeleter
{
    /// <returns>`true` if deletion request completed without error, else `false`</returns>
    Task<bool> DeleteDonorsFromDonorStore(IEnumerable<string> externalDonorCodes);
}

internal class DonorDeleter : IDonorDeleter
{
    private readonly IDebugRequester debugRequester;
    private readonly IDonorImportFunctionsClient donorImportClient;

    public DonorDeleter(
        IDebugRequester debugRequester, 
        IDonorImportFunctionsClient donorImportClient)
    {
        this.debugRequester = debugRequester;
        this.donorImportClient = donorImportClient;
    }

    public async Task<bool> DeleteDonorsFromDonorStore(IEnumerable<string> externalDonorCodes)
    {
        return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(
            3, 10, async () => await DeleteDonors(externalDonorCodes));
    }

    private async Task<bool> DeleteDonors(IEnumerable<string> externalDonorCodes)
    {
        await donorImportClient.DeleteDonors(externalDonorCodes);
        return true;
    }
}