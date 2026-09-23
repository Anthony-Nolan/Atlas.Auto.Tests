namespace Atlas.Auto.Tests.TestHelpers.Data;

internal class HlaExpansionFailure
{
    public string InvalidHLA { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public IEnumerable<string> ExternalDonorCodes { get; set; } = [];
    public long DonorCount { get; set; }
}
