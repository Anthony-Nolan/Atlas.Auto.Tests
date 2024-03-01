using Atlas.Auto.Tests.TestHelpers.Services.DonorImport;

namespace Atlas.Auto.Tests.TestHelpers.Services.DonorDeletion
{
    internal interface ITestDonorDeleter
    {
        /// <summary>
        /// No exception will be thrown if any of the deletion attempts fails, as this is not a critical operation.
        /// </summary>
        Task DeleteDonors();
    }

    internal class TestDonorDeleter : ITestDonorDeleter
    {
        private readonly IDonorCodeFetcher donorCodeFetcher;
        private readonly IDonorDeleter donorDeleter;
        private readonly IAvailabilitySetter availabilitySetter;

        public TestDonorDeleter(
            IDonorCodeFetcher donorCodeFetcher,
            IDonorDeleter donorDeleter,
            IAvailabilitySetter availabilitySetter)
        {
            this.donorCodeFetcher = donorCodeFetcher;
            this.donorDeleter = donorDeleter;
            this.availabilitySetter = availabilitySetter;
        }

        public async Task DeleteDonors()
        {
            var donorCodes = await GetAutoTestDonorCodes();

            if (!donorCodes.Any())
            {
                Console.WriteLine(BuildLog("No donor codes were returned for deletion"));
                return;
            }

            // Note: This implementation of calling debug endpoints to directly delete/disable donors is quicker
            // than building a diff file with all the donors to delete and then importing it.

            var deleteResult = await donorDeleter.DeleteDonorsFromDonorStore(donorCodes);
            Console.WriteLine(BuildLogByOutcome("Donors deletion from donor store", deleteResult));

            var availabilityResult = await availabilitySetter.SetDonorsAsUnavailableForSearch(donorCodes);
            Console.WriteLine(BuildLogByOutcome("Setting donors as unavailable for search", availabilityResult));
        }

        private async Task<IReadOnlyCollection<string>> GetAutoTestDonorCodes()
        {
            // Only fetch donors that were updated before today.
            // This is to account for the possibility of multiple developers/pipelines running tests against the same environment at the same time.
            // We don't want to delete newly added test donors that are still being used by other runs.
            var updatedBeforeDate = DateTime.UtcNow.ToString("yyyyMMdd");
            var donorCodeResponse = await donorCodeFetcher.FetchAutoTestDonorCodes(updatedBeforeDate);

            if (donorCodeResponse.WasSuccess)
            {
                return donorCodeResponse.DebugResult?.ToList() ?? new List<string>();
            }

            Console.WriteLine(BuildLog("Failed to fetch donor codes for deletion"));
            return new List<string>();
        }

        private static string BuildLogByOutcome(string action, bool outcome) =>
            BuildLog($"{action} {(outcome ? "was successful" : "failed")}");

        private static string BuildLog(string message) => $"{nameof(TestDonorDeleter)}: {message}";
    }
}
