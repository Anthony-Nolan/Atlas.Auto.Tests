using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services.DonorImport;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Workflows;

internal interface IDonorImportWorkflow
{
    Task<bool> ImportDonorFile(DonorImportRequest request);
    Task<DebugResponse<DonorImportMessage>> FetchResultMessage(string fileName);
    Task<DebugResponse<DebugDonorsResult>> CheckDonorsInDonorStore(IEnumerable<string> externalDonorCodes);
    Task<DebugResponse<DebugDonorsResult>> CheckAllDonorsArePresent(IEnumerable<string> externalDonorCodes);
}

internal class DonorImportWorkflow : IDonorImportWorkflow
{
    private readonly IFileImporter fileImporter;
    private readonly IImportResultFetcher importResultFetcher;
    private readonly IDonorStoreChecker donorStoreChecker;
    private readonly IActiveMatchingDbChecker activeMatchingDbChecker;

    public DonorImportWorkflow(
        IFileImporter fileImporter,
        IImportResultFetcher importResultFetcher,
        IDonorStoreChecker donorStoreChecker,
        IActiveMatchingDbChecker activeMatchingDbChecker)
    {
        this.fileImporter = fileImporter;
        this.importResultFetcher = importResultFetcher;
        this.donorStoreChecker = donorStoreChecker;
        this.activeMatchingDbChecker = activeMatchingDbChecker;
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

    public async Task<DebugResponse<DebugDonorsResult>> CheckAllDonorsArePresent(IEnumerable<string> externalDonorCodes)
    {
        return await activeMatchingDbChecker.CheckAllDonorsArePresent(externalDonorCodes);
    }
}