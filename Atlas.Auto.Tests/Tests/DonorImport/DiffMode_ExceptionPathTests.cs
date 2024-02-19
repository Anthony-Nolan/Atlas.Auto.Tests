using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover exception paths of Atlas donor import when in diff mode.
/// </summary>
[TestFixture]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(DiffMode_ExceptionPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class DiffMode_ExceptionPathTests
{
    private IServiceProvider serviceProvider;
    private IDonorImportWorkflow donorImportWorkflow;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        serviceProvider = ServiceConfiguration.CreateProvider();
    }

    [SetUp]
    public void SetUp()
    {
        donorImportWorkflow = serviceProvider.ResolveServiceOrThrow<IDonorImportWorkflow>();
    }

    [Test]
    public async Task DonorImport_DiffMode_CreateExistingDonor_FailsTheInvalidUpdate()
    {
        const int donorCount = 1;
        const ImportDonorChangeType changeType = ImportDonorChangeType.Create;

        // import the same donor create update twice

        // first time: both import and creation update should succeed
        var creationUpdate = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(changeType)
            .Build(donorCount);

        var firstImportRequest = await donorImportWorkflow.ImportDiffDonorFile(creationUpdate);
        await donorImportWorkflow.DonorImportWasSuccessful(firstImportRequest.FileName, donorCount, 0);

        var expectedDonorInfo = creationUpdate.ToDonorDebugInfo().ToList();
        await donorImportWorkflow.DonorStoreShouldHaveExpectedDonors(expectedDonorInfo);
        await donorImportWorkflow.DonorsShouldBeAvailableForSearch(expectedDonorInfo);

        // second time: the import should succeed, but the update should fail validation
        var secondImportRequest = await donorImportWorkflow.ImportDiffDonorFile(creationUpdate);
        await donorImportWorkflow.DonorImportWasSuccessful(secondImportRequest.FileName, 0, donorCount);
    }
}