using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.Auto.Tests.TestHelpers.Builders;

internal static class DonorImportRequestBuilder
{
    public static Builder<DonorImportRequest> New => Builder<DonorImportRequest>.New
        .WithFactory(m => m.FileName, DonorImportGenerators.BuildFileName);

    public static Builder<DonorImportRequest> WithDiffModeFile(
        this Builder<DonorImportRequest> builder,
        IEnumerable<DonorUpdate> donorUpdates)
    {
        var contents = DonorImportFileContentsBuilder.DiffMode.WithDonorUpdates(donorUpdates).Build();

        return builder
            .WithFactory(m => m.FileContents, () => contents);
    }
}