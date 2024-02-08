using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover happy paths of Atlas donor import when in diff mode.
/// </summary>
[TestFixture]
// ReSharper disable once InconsistentNaming
internal class DiffMode_HappyPathTests
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
    public async Task DonorImport_DiffMode_Create_CreatedDonorsSuccessfully()
    {
        const int donorCount = 2;
        const ImportDonorChangeType changeType = ImportDonorChangeType.Create;

        var updates = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeType(changeType)
            .Build(donorCount);

        var request = DonorImportRequestBuilder.New.WithDiffModeFile(updates).Build();

        var importResponse = await donorImportWorkflow.ImportDonorFile(request);
        importResponse.Should().BeTrue("the file should have been sent to Atlas");

        var fetchResultResponse = await donorImportWorkflow.FetchResultMessage(request.FileName);
        fetchResultResponse.ShouldBeSuccessful();

        var importResultMessage = fetchResultResponse.DebugResult;
        importResultMessage?.ImportWasSuccessful();
        importResultMessage?.ShouldHaveImportedDonorCount(donorCount);
        importResultMessage?.ShouldHaveFailedDonorCount(0);

        var externalDonorCodes = updates.GetExternalDonorCodes();

        var donorStoreCheckResponse = await donorImportWorkflow.CheckDonorsInDonorStore(externalDonorCodes);
        donorStoreCheckResponse.ShouldBeSuccessful();

        var donorStoreCheckResult = donorStoreCheckResponse.DebugResult;
        donorStoreCheckResult?.ShouldHaveAllExpectedDonors(externalDonorCodes);
        // todo #9: assert that info for present donors is correct

        var matchingDbCheckResponse = await donorImportWorkflow.CheckAllDonorsArePresent(externalDonorCodes);
        matchingDbCheckResponse.ShouldBeSuccessful();

        var matchingDbCheckResult = matchingDbCheckResponse.DebugResult;
        matchingDbCheckResult?.ShouldHaveAllExpectedDonors(externalDonorCodes);
        // todo #9: assert that info for present donors is correct
    }
}