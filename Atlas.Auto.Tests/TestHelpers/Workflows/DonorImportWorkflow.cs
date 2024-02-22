using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.DonorImport;
using Atlas.Client.Models.SupportMessages;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

/// <summary>
/// Workflow to import a donor file and determine the outcome of import.
/// </summary>
internal interface IDonorImportWorkflow
{
    Task<DebugResponse<bool>> IsFullModeImportAllowed();
    Task<bool> ImportDonorFile(DonorImportRequest request);
    Task<DebugResponse<DonorImportMessage>> FetchResultMessage(string fileName);
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes);
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreAvailableForSearch(IEnumerable<string> externalDonorCodes);
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsAreNotAvailableForSearch(IEnumerable<string> externalDonorCodes);
    Task<DebugResponse<Alert>> FetchFailedFileAlert(string fileName);
    Task<DebugResponse<Alert>> FetchHlaExpansionFailureAlert();
    Task<DebugResponse<DonorImportFailureInfo>> FetchDonorImportFailureInfo(string fileName);
}

internal class DonorImportWorkflow : IDonorImportWorkflow
{
    private readonly IFileImporter fileImporter;
    private readonly IImportResultFetcher importResultFetcher;
    private readonly IDonorStoreChecker donorStoreChecker;
    private readonly IActiveMatchingDbChecker activeMatchingDbChecker;
    private readonly IFullModeChecker fullModeChecker;
    private readonly IFailedFileAlertFetcher failedFileAlertFetcher;
    private readonly IHlaExpansionFailureAlertFetcher hlaExpansionFailureAlertFetcher;
    private readonly IDonorImportFailureInfoFetcher donorImportFailureInfoFetcher;

    public DonorImportWorkflow(
        IFullModeChecker fullModeChecker,
        IFileImporter fileImporter,
        IImportResultFetcher importResultFetcher,
        IDonorStoreChecker donorStoreChecker,
        IActiveMatchingDbChecker activeMatchingDbChecker,
        IFailedFileAlertFetcher failedFileAlertFetcher,
        IHlaExpansionFailureAlertFetcher hlaExpansionFailureAlertFetcher,
        IDonorImportFailureInfoFetcher donorImportFailureInfoFetcher)
    {
        this.fullModeChecker = fullModeChecker;
        this.fileImporter = fileImporter;
        this.importResultFetcher = importResultFetcher;
        this.donorStoreChecker = donorStoreChecker;
        this.activeMatchingDbChecker = activeMatchingDbChecker;
        this.failedFileAlertFetcher = failedFileAlertFetcher;
        this.hlaExpansionFailureAlertFetcher = hlaExpansionFailureAlertFetcher;
        this.donorImportFailureInfoFetcher = donorImportFailureInfoFetcher;
    }

    public async Task<DebugResponse<bool>> IsFullModeImportAllowed()
    {
        return await fullModeChecker.IsFullModeImportAllowed();
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

    public async Task<DebugResponse<Alert>> FetchFailedFileAlert(string fileName)
    {
        return await failedFileAlertFetcher.FetchAlertMessage(fileName);
    }

    public async Task<DebugResponse<Alert>> FetchHlaExpansionFailureAlert()
    {
        return await hlaExpansionFailureAlertFetcher.FetchAlertMessage();
    }

    public async Task<DebugResponse<DonorImportFailureInfo>> FetchDonorImportFailureInfo(string fileName)
    {
        return await donorImportFailureInfoFetcher.FetchFailureInfo(fileName);
    }
}