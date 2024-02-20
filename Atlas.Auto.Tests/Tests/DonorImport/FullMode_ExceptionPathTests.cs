using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover exception paths of Atlas donor import when in Full mode.
/// </summary>
[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(FullMode_ExceptionPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class FullMode_ExceptionPathTests
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
    public async Task DonorImport_DoesNotAllowFullModeImport()
    {
        var response = await donorImportWorkflow.IsFullModeImportAllowed();

        // The debug http response should have been successful,
        // but the embedded result should be false as full mode import should not be allowed.
        response.WasSuccess.Should().BeTrue();
        response.DebugResult.Should().BeFalse();
    }

    [Test]
    public async Task DonorImport_FullMode_Create_FailsEntireImport()
    {
        const int donorCount = 2;
        var updates = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await donorImportWorkflow.ImportFullDonorFile(updates);
        await donorImportWorkflow.DonorImportShouldHaveFailed(request.FileName);
        await donorImportWorkflow.ShouldHaveRaisedAlertForFullModeImport(request.FileName);
        await donorImportWorkflow.DonorStoreShouldNotHaveTheseDonors(updates.GetExternalDonorCodes());
    }
}