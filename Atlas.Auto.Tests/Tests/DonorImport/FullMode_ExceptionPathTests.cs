using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

/// <summary>
/// Tests that cover exception paths of Atlas donor import when in Full mode.
/// </summary>
[TestFixture]
[Parallelizable(scope: ParallelScope.All)]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(FullMode_ExceptionPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class FullMode_ExceptionPathTests : DonorImportTestBase
{
    public FullMode_ExceptionPathTests() : base(nameof(FullMode_ExceptionPathTests))
    {
    }

    [Test]
    public async Task DonorImport_DoesNotAllowFullModeImport()
    {
        var test = GetTestServices(nameof(DonorImport_DoesNotAllowFullModeImport));
        await test.Steps.FullModeImportShouldNotBeAllowed();
    }

    [Test]
    public async Task DonorImport_FullMode_Create_FailsEntireImport()
    {
        const string testCase = "attempt to create donors in full mode";
        const int donorCount = 2;
        var test = GetTestServices(nameof(DonorImport_FullMode_Create_FailsEntireImport));

        test.Logger.LogStart(testCase);

        var updates = DonorUpdateBuilder.Default
            .WithValidDnaAtAllLoci()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await test.Steps.ImportFullDonorFile(updates);
        await test.Steps.DonorImportShouldHaveFailed(request.FileName);
        await test.Steps.FullModeImportAlertShouldHaveBeenRaised(request.FileName);
        await test.Steps.DonorStoreShouldNotHaveTheseDonors(updates.GetExternalDonorCodes());

        test.Logger.LogCompletion(testCase);
    }
}