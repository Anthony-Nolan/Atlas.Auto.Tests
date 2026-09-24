namespace Atlas.Auto.Tests.TestHelpers.Settings;

internal class ServiceBusSettings
{
    public string ConnectionString { get; set; } = "override-this";
    public string Subscription { get; set; } = "debug";

    public string DonorImportResultsTopic { get; set; } = "donor-import-results";
    public string AlertsTopic { get; set; } = "alerts";
    public string MatchingResultsTopic { get; set; } = "matching-results-ready";
    public string SearchResultsTopic { get; set; } = "search-results-ready";
    public string RepeatSearchMatchingResultsTopic { get; set; } = "repeat-search-matching-results-ready";
    public string RepeatSearchResultsTopic { get; set; } = "repeat-search-results-ready";
}
