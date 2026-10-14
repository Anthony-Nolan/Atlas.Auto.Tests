namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class DonorImportRequest
{
    public string FileName { get; set; } = string.Empty;
    public DonorImportFile FileContents { get; set; } = new();

    /// <summary>
    /// Test machine time just before the file was uploaded.
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }
}
