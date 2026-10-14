namespace Atlas.Auto.Tests.TestHelpers.Data;

/// <summary>
/// Message published to updated-searchable-donors once a donor change has been applied.
/// Mirrors Atlas.DonorImport.ExternalInterface.Models.SearchableDonorUpdate (only the fields the tests need).
/// </summary>
internal class SearchableDonorUpdate
{
    public DateTimeOffset PublishedDateTime { get; set; }
    public int DonorId { get; set; }
    public bool IsAvailableForSearch { get; set; }
    public SearchableDonorInformation? SearchableDonorInformation { get; set; }
}

internal class SearchableDonorInformation
{
    public string ExternalDonorCode { get; set; } = string.Empty;
}
