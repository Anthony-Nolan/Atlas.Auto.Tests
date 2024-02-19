using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.DonorImport;
using Atlas.Client.Models.SupportMessages;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

internal interface IDonorImportWorkflow
{
    Task<bool> ImportDonorFile(DonorImportRequest request);
    Task<DebugResponse<DonorImportMessage>> FetchResultMessage(string fileName);
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes);
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreAvailableForSearch(IEnumerable<string> externalDonorCodes);
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreNotAvailableForSearch(IEnumerable<string> externalDonorCodes);
    Task<DebugResponse<bool>> IsFullModeImportAllowed();
    Task<DebugResponse<Alert>> FetchAlert(string fileName);
}

internal class DonorImportWorkflow : IDonorImportWorkflow
{
    private readonly IFileImporter fileImporter;
    private readonly IImportResultFetcher importResultFetcher;
    private readonly IDonorStoreChecker donorStoreChecker;
    private readonly IActiveMatchingDbChecker activeMatchingDbChecker;
    private readonly IFullModeChecker fullModeChecker;
    private readonly IFailedFileAlertFetcher alertFetcher;

    public DonorImportWorkflow(
        IFileImporter fileImporter,
        IImportResultFetcher importResultFetcher,
        IDonorStoreChecker donorStoreChecker,
        IActiveMatchingDbChecker activeMatchingDbChecker,
        IFullModeChecker fullModeChecker,
        IFailedFileAlertFetcher alertFetcher)
    {
        this.fileImporter = fileImporter;
        this.importResultFetcher = importResultFetcher;
        this.donorStoreChecker = donorStoreChecker;
        this.activeMatchingDbChecker = activeMatchingDbChecker;
        this.fullModeChecker = fullModeChecker;
        this.alertFetcher = alertFetcher;
    }

    public async Task<bool> ImportDonorFile(DonorImportRequest request)
    {
        return await fileImporter.Import(request);
    }

    public async Task<DebugResponse<DonorImportMessage>> FetchResultMessage(string fileName)
    {
        return await importResultFetcher.FetchResultMessage(fileName);
    }

    public async Task<DebugResponse<DebugDonorsResult>> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes)
    {
        return await donorStoreChecker.Check(externalDonorCodes);
    }

    public async Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        return await activeMatchingDbChecker.CheckDonorsAreAvailableForSearch(externalDonorCodes);
    }

    public async Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreNotAvailableForSearch(IEnumerable<string> externalDonorCodes)
    {
        return await activeMatchingDbChecker.CheckDonorsAreNotAvailableForSearch(externalDonorCodes);
    }

    public async Task<DebugResponse<bool>> IsFullModeImportAllowed()
    {
        return await fullModeChecker.IsFullModeImportAllowed();
    }

    public async Task<DebugResponse<Alert>> FetchAlert(string fileName)
    {
        return await alertFetcher.FetchAlertMessage(fileName);
    }
}