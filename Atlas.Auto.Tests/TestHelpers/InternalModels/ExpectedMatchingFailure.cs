namespace Atlas.Auto.Tests.TestHelpers.InternalModels;

/// <summary>
/// How search tracking should record a failed matching algorithm run: failure type (Atlas MatchingAlgorithmFailureType) and message.
/// </summary>
internal record ExpectedMatchingFailure(string Type, string Message)
{
    /// <summary>Patient HLA the HLA metadata dictionary does not know.</summary>
    public static ExpectedMatchingFailure HlaLookup(string hla, string locus) =>
        new("HlaMetadataDictionaryError", $"Failed to lookup '{hla}' at locus {locus}.");

    /// <summary>Repeat search of a search Atlas has no record of.</summary>
    public static ExpectedMatchingFailure NoOriginalSearch(string originalSearchId) =>
        new("ValidationError",
            $"Can not run a repeat search for search request with Id: {originalSearchId}, as there is no record of the original search.");
}
