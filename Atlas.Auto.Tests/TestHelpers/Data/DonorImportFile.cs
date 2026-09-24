using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class DonorImportFile : DonorImportFileSchema
{
    public override UpdateMode updateMode { get; set; }
    public override IEnumerable<DonorUpdate> donors { get; set; } = [];
}
