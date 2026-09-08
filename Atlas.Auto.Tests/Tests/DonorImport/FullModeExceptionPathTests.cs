using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.Tests.DonorImport;

[TestFixture]
[Category($"{TestConstants.DonorImportTestTag}_{nameof(FullModeExceptionPathTests)}")]
internal class FullModeExceptionPathTests : DonorImportTestBase
{
    public FullModeExceptionPathTests() : base(nameof(FullModeExceptionPathTests))
    {
    }

    [Test]
    public async Task DonorImport_DoesNotAllowFullModeImport()
    {
        const string testCase = "full mode import should not be allowed";
        var steps = GetDonorImportTestSteps(nameof(DonorImport_DoesNotAllowFullModeImport));
        steps.Logger.LogStart(testCase);
        await steps.FullModeImportShouldNotBeAllowed();
        steps.Logger.LogCompletion(testCase);
    }

    [Test]
    public async Task DonorImport_FullMode_Create_FailsEntireImport()
    {
        const string testCase = "attempt to create donors in full mode";
        const int donorCount = 2;
        var steps = GetDonorImportTestSteps(nameof(DonorImport_FullMode_Create_FailsEntireImport));

        steps.Logger.LogStart(testCase);

        var updates = DonorUpdateBuilder.Default
            .WithValidDnaPhenotype()
            .WithChangeTypes(new[] { ImportDonorChangeType.Create, ImportDonorChangeType.Upsert })
            .Build(donorCount);

        var request = await steps.ImportFullDonorFile(updates);
        await steps.DonorImportShouldHaveFailed(request.FileName);
        await steps.FullModeImportAlertShouldHaveBeenRaised(request.FileName);
        await steps.DonorStoreShouldNotHaveTheseDonors(updates.GetExternalDonorCodes());

        steps.Logger.LogCompletion(testCase);
    }
}
