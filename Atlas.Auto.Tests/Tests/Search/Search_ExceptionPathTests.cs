using Atlas.Auto.Tests.TestHelpers.Assertions;

namespace Atlas.Auto.Tests.Tests.Search;

/// <summary>
/// Tests that cover exception paths of Atlas search.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.All)]
[Category($"{nameof(Search_ExceptionPathTests)}")]
// ReSharper disable once InconsistentNaming
internal class Search_ExceptionPathTests : SearchTestBase
{
    public Search_ExceptionPathTests() : base(nameof(Search_ExceptionPathTests))
    {
    }

    [Test]
    public async Task Search_MissingRequiredInfo_ReturnsValidationError()
    {
        var test = GetSearchTestServices(nameof(Search_MissingRequiredInfo_ReturnsValidationError));

        const string testDescription = "Search with missing required information";
        test.Logger.LogStart(testDescription);

        var validationFailures = await test.Steps.SubmitInvalidSearchRequest("search-request-missing-locus-A.json");
        validationFailures.ToList().ShouldContain(@"'A' must not be empty.");

        test.Logger.LogCompletion(testDescription);
    }

    [Test]
    public async Task Search_InvalidPatientHla_FailsDuringMatching()
    {
        var test = GetSearchTestServices(nameof(Search_InvalidPatientHla_FailsDuringMatching));

        const string testDescription = "Search with invalid patient HLA";
        test.Logger.LogStart(testDescription);

        var searchResponse = await test.Steps.SubmitSearchRequest("search-request-invalid-patient-hla-at-A1.json");
        await test.Steps.MatchingShouldFailHlaValidation(searchResponse.SearchIdentifier);

        test.Logger.LogCompletion(testDescription);
    }
}