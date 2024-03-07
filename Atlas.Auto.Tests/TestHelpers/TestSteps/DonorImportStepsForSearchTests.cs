using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

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
        /// Creates a donor of the specified type with <see cref="HlaTypings.ValidDnaPhenotype"/> and returns the donor record id.
        /// </summary>
        Task<string> CreateDonorWithValidDnaPhenotype(ImportDonorType donorType);

        /// <summary>
        /// Edits the HLA of donor with code <see cref="donorCode"/> to <see cref="HlaTypings.SearchTestPhenotype"/>.
        /// </summary>
        Task EditDonorHlaToSearchTestPhenotype(string donorCode, ImportDonorType donorType);

        /// <summary>
        /// Edits the HLA of donor with code <see cref="donorCode"/> to <see cref="HlaTypings.ValidDnaPhenotype"/>.
        /// </summary>
        Task EditDonorHlaToValidDnaPhenotype(string donorCode, ImportDonorType donorType);

        /// <summary>
        /// Deletes donors.
        /// </summary>
        Task DeleteDonors(IReadOnlyCollection<string> donorCodes);
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
            return await CreateDonor(donorType, ImportedHlaBuilder.SearchTestPhenotype);
        }

        /// <inheritdoc />
        public async Task<string> CreateDonorWithValidDnaPhenotype(ImportDonorType donorType)
        {
            return await CreateDonor(donorType, ImportedHlaBuilder.ValidDnaPhenotype);
        }

        /// <inheritdoc />
        public async Task EditDonorHlaToSearchTestPhenotype(string donorCode, ImportDonorType donorType)
        {
            await EditDonorHla(donorCode, donorType, ImportedHlaBuilder.SearchTestPhenotype);
        }

        /// <inheritdoc />
        public async Task EditDonorHlaToValidDnaPhenotype(string donorCode, ImportDonorType donorType)
        {
            await EditDonorHla(donorCode, donorType, ImportedHlaBuilder.ValidDnaPhenotype);
        }

        /// <inheritdoc />
        public async Task DeleteDonors(IReadOnlyCollection<string> donorCodes)
        {
            const string action = "Delete test donors";
            logger.LogStart(action);

            var donorUpdate = DonorUpdateBuilder.Default
                .WithRecordIds(donorCodes)
                .WithChangeType(ImportDonorChangeType.Delete)
                .Build(donorCodes.Count);

            var request = await donorImportTestSteps.ImportDiffDonorFile(donorUpdate);
            await donorImportTestSteps.DonorImportShouldHaveBeenSuccessful(request.FileName, donorCodes.Count, 0);
            await donorImportTestSteps.DonorStoreShouldNotHaveTheseDonors(donorCodes);
            await donorImportTestSteps.DonorsShouldNotBeAvailableForSearch(donorCodes);

            logger.LogCompletion(action);
        }

        private async Task<string> CreateDonor(ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
        {
            var action = $"Create test {donorType}";
            logger.LogStart(action);

            const int donorCount = 1;
            var donorUpdate = DonorUpdateBuilder.Default
                .WithDonorType(donorType)
                .WithHla(hlaBuilder)
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

        private async Task EditDonorHla(string donorCode, ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
        {
            var action = $"Edit HLA of test {donorType} with record id {donorCode}";
            logger.LogStart(action);

            const int donorCount = 1;
            var donorUpdate = DonorUpdateBuilder.Default
                .WithRecordIds(new[] { donorCode })
                .WithDonorType(donorType)
                .WithHla(hlaBuilder)
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
    }
}
