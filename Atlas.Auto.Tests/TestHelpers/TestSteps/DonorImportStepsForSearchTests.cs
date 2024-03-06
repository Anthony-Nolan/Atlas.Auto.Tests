using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps
{
    /// <summary>
    /// Steps to perform donor import for search and repeat search tests.
    /// </summary>
    internal interface IDonorImportStepsForSearchTests
    {
        /// <summary>
        /// Creates a donor of the specified type with <see cref="HlaTypings.SearchTestPhenotype"/> and returns the donor record id.
        /// </summary>
        Task<string> CreateDonorWithSearchTestPhenotype(ImportDonorType donorType);

        /// <summary>
        /// Edits the HLA of donor with code <see cref="donorCode"/> to a phenotype that does not match <see cref="HlaTypings.SearchTestPhenotype"/>.
        /// </summary>
        Task EditDonorHlaToNonMatchingPhenotype(string donorCode, ImportDonorType donorType);

        /// <summary>
        /// Deletes the donor with code <see cref="donorCode"/>.
        /// </summary>
        Task DeleteDonor(string donorCode);
    }

    internal class DonorImportStepsForSearchTests : IDonorImportStepsForSearchTests
    {
        private readonly IDonorImportTestSteps donorImportTestSteps;
        private readonly ITestLogger logger;

        public DonorImportStepsForSearchTests(
            IDonorImportTestSteps donorImportTestSteps,
            ITestLogger logger)
        {
            this.donorImportTestSteps = donorImportTestSteps;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> CreateDonorWithSearchTestPhenotype(ImportDonorType donorType)
        {
            var action = $"Create test {donorType}";
            logger.LogStart(action);

            const int donorCount = 1;
            var donorUpdate = DonorUpdateBuilder.Default
                .WithSearchTestPhenotype()
                .WithDonorType(donorType)
                .WithChangeType(ImportDonorChangeType.Create)
                .Build(donorCount);

            var request = await donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
            await donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);

            var donorInfo = donorUpdate.ToDonorDebugInfo().ToList();
            await donorImportTestSteps.DonorStoreShouldHaveExpectedDonors(donorInfo);
            await donorImportTestSteps.DonorsShouldBeAvailableForSearch(donorInfo);

            var recordId = donorUpdate.Single().RecordId;
            logger.LogInfo($"Donor record id: {recordId}");
            logger.LogCompletion(action);

            return recordId;
        }

        /// <inheritdoc />
        public async Task EditDonorHlaToNonMatchingPhenotype(string donorCode, ImportDonorType donorType)
        {
            var action = $"Edit HLA of test {donorType} with record id {donorCode}";
            logger.LogStart(action);

            const int donorCount = 1;
            var donorUpdate = DonorUpdateBuilder.Default
                .WithValidDnaPhenotype()
                .WithRecordIds(new[] { donorCode })
                .WithDonorType(donorType)
                .WithChangeType(ImportDonorChangeType.Edit)
                .Build(donorCount);

            var request = await donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
            await donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);

            var donorInfo = donorUpdate.ToDonorDebugInfo().ToList();
            await donorImportTestSteps.DonorStoreShouldHaveExpectedDonors(donorInfo);
            // Purposefully not checking if donor is available for search, as only changed the HLA,
            // and current approach depends on change in donor availability to determine when matching algorithm has caught up with donor updates.
            // todo #17: extend active matching db checker logic to check for changes in HLA.

            logger.LogCompletion(action);
        }

        /// <inheritdoc />
        public async Task DeleteDonor(string donorCode)
        {
            var action = $"Delete test donor of record id {donorCode}";
            logger.LogStart(action);

            const int donorCount = 1;
            var donorCodes = new[] { donorCode };
            var donorUpdate = DonorUpdateBuilder.Default
                .WithRecordIds(donorCodes)
                .WithChangeType(ImportDonorChangeType.Delete)
                .Build(donorCount);

            var request = await donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
            await donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCount, 0);
            await donorImportTestSteps.DonorStoreShouldNotHaveTheseDonors(donorCodes);
            await donorImportTestSteps.DonorsShouldNotBeAvailableForSearch(donorCodes);

            logger.LogCompletion(action);
        }
    }
}
