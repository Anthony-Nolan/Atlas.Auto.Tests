using Atlas.Debug.Client.Clients;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorImport
{
    internal interface IFileImporter
    {
        Task<bool> Import(DonorImportRequest request);
    }

    internal class FileImporter : IFileImporter
    {
        private readonly IDebugRequester debugRequester;
        private readonly IDonorImportFunctionsClient donorImportClient;

        public FileImporter(IDebugRequester debugRequester, IDonorImportFunctionsClient donorImportClient)
        {
            this.debugRequester = debugRequester;
            this.donorImportClient = donorImportClient;
        }

        public async Task<bool> Import(DonorImportRequest request)
        {
            return await debugRequester.ExecuteDebugRequestWithWaitAndRetry(5, 5, async () => await SendFile(request));
        }

        /// <returns>Returns `true` if import request completes successfully. Does not return `false`, instead request failure will throw.</returns>
        private async Task<bool> SendFile(DonorImportRequest request)
        {
            await donorImportClient.ImportFile(request);
            return true;
        }
    }
}
