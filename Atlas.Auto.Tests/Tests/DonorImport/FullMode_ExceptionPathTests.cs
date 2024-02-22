using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using Atlas.DonorImport.FileSchema.Models;

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

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        serviceProvider = ServiceConfiguration.CreateProvider();
    }

    [Test]
    public async Task DonorImport_DoesNotAllowFullModeImport()
    {
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();
        await testSteps.FullModeImportShouldNotBeAllowed();
    }

    [Test]
    public async Task DonorImport_FullMode_Create_FailsEntireImport()
    {
        const int donorCount = 2;
        var testSteps = serviceProvider.ResolveServiceOrThrow<IDonorImportTestSteps>();

        var updates = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await testSteps.ImportFullDonorFile(updates);
        await testSteps.DonorImportShouldHaveFailed(request.FileName);
        await testSteps.FullModeImportAlertShouldHaveBeenRaised(request.FileName);
        await testSteps.DonorStoreShouldNotHaveTheseDonors(updates.GetExternalDonorCodes());
    }
}