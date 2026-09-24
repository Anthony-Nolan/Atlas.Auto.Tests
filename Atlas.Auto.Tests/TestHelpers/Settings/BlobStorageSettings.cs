namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class BlobStorageSettings
{
    public string ConnectionString { get; set; } = "override-this";
    public string DonorFileContainer { get; set; } = "donors";
}
