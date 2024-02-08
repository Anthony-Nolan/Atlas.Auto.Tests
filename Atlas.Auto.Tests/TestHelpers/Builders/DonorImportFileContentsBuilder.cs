using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using LochNessBuilder;

namespace Atlas.Auto.Tests.TestHelpers.Builders;

internal static class DonorImportFileContentsBuilder
{
    public static Builder<DonorImportFileContents> DiffMode => Builder<DonorImportFileContents>.New
        .With(d => d.updateMode, UpdateMode.Differential);

    public static Builder<DonorImportFileContents> WithDonorUpdates(
        this Builder<DonorImportFileContents> donorUpdateBuilder,
        IEnumerable<DonorUpdate> donorUpdates)
        => donorUpdateBuilder.WithFactory(d => d.donors, () => donorUpdates);
}