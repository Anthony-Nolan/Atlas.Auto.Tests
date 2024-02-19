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
        var contentBuilder = DonorImportFileContentsBuilder.DiffMode.WithDonorUpdates(donorUpdates);
        return builder.WithContents(contentBuilder);
    }

    public static Builder<DonorImportRequest> WithFullModeFile(
        this Builder<DonorImportRequest> builder,
        IEnumerable<DonorUpdate> donorUpdates)
    {
        var contentBuilder = DonorImportFileContentsBuilder.FullMode.WithDonorUpdates(donorUpdates);
        return builder.WithContents(contentBuilder);
    }

    private static Builder<DonorImportRequest> WithContents(
        this Builder<DonorImportRequest> builder,
        Builder<DonorImportFileContents> contentBuilder)
        => builder.WithFactory(m => m.FileContents, contentBuilder.Build);
}