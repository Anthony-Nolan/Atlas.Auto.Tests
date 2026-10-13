namespace Atlas.Auto.Tests.TestHelpers.Data;

/// <summary>
/// Event Grid event published to donor-import-file-uploads when a file lands in the donors container.
/// Mirrors Atlas.Common.AzureEventGrid.EventGridSchema.
/// </summary>
internal class BlobCreatedEvent
{
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset EventTime { get; set; }
    public BlobCreatedEventData? Data { get; set; }
}

internal class BlobCreatedEventData
{
    public string Url { get; set; } = string.Empty;
}
