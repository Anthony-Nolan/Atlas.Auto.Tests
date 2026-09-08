using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.Auto.Tests.TestHelpers.Builders;

internal static class DonorImportRequestBuilder
{
    public static Builder<DonorImportRequest> New => Builder<DonorImportRequest>.New
        .WithFactory(m => m.FileName, () => $"{TestConstants.AutoTestTag}_{Guid.NewGuid()}.json");

    public static Builder<DonorImportRequest> WithDiffModeFile(
        this Builder<DonorImportRequest> builder,
        IEnumerable<DonorUpdate> donorUpdates)
        => builder.WithContents(UpdateMode.Differential, donorUpdates);

    public static Builder<DonorImportRequest> WithFullModeFile(
        this Builder<DonorImportRequest> builder,
        IEnumerable<DonorUpdate> donorUpdates)
        => builder.WithContents(UpdateMode.Full, donorUpdates);

    private static Builder<DonorImportRequest> WithContents(
        this Builder<DonorImportRequest> builder,
        UpdateMode mode,
        IEnumerable<DonorUpdate> donorUpdates)
    {
        var contentBuilder = Builder<DonorImportFileContents>.New
            .With(d => d.updateMode, mode)
            .WithFactory(d => d.donors, () => donorUpdates);
        return builder.WithFactory(m => m.FileContents, contentBuilder.Build);
    }
}
