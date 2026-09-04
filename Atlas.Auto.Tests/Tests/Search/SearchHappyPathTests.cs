using Atlas.Auto.Tests.TestHelpers.Builders;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.DonorImport.FileSchema.Models;
using System.Runtime.CompilerServices;

namespace Atlas.Auto.Tests.Tests.Search;

[TestFixture]
[Category($"{TestConstants.SearchTestTag}_{nameof(SearchHappyPathTests)}")]
internal class SearchHappyPathTests : SearchTestBase
{
    public SearchHappyPathTests() : base(nameof(SearchHappyPathTests))
    {
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Search_Donor_10_10_ReturnsExpectedSearchResult(bool? parallelMatchPrediction)
    {
        await Search_Patient(
            testDescription: "10/10 Donor Search",
            importDonorType: ImportDonorType.Adult,
            searchRequestJson: "search-request-donor-10_10.json",
            parallelMatchPrediction: parallelMatchPrediction);
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Search_Cord_4_8_ReturnsExpectedSearchResult(bool? parallelMatchPrediction)
    {
        await Search_Patient(
            testDescription: "4/8 Cord Search",
            importDonorType: ImportDonorType.Cord,
            searchRequestJson: "search-request-cord-4_8.json",
            parallelMatchPrediction: parallelMatchPrediction);
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Search_DonorWithNew_9_10_ReturnsExpectedSearchResult(bool? parallelMatchPrediction)
    {
        await Search_Patient(
            testDescription: "9/10 Donor Search",
            importDonorType: ImportDonorType.Adult,
            searchRequestJson: "search-request-donor-9_10.json",
            newDnaPhenotype: true,
            parallelMatchPrediction: parallelMatchPrediction);
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Search_PatientWithNew_9_10_ReturnsExpectedSearchResult(bool? parallelMatchPrediction)
    {
        await Search_Patient(
            testDescription: "9/10 Donor Search - Patient with New",
            importDonorType: ImportDonorType.Adult,
            searchRequestJson: "search-request-patient-with-new-at-A1.json",
            parallelMatchPrediction: parallelMatchPrediction);
    }

    [TestCaseSource(nameof(Cases))]
    public async Task Search_PatientWithNew_9_10_ReturnsExpectedSearchResultDonorWithNew(bool? parallelMatchPrediction)
    {
        await Search_Patient(
            testDescription: "9/10 Donor Search - Patient with New and Donor with New",
            importDonorType: ImportDonorType.Adult,
            searchRequestJson: "search-request-patient-with-new-at-A1.json",
            newDnaPhenotype: true,
            parallelMatchPrediction: parallelMatchPrediction);
    }

    [Test]
    public async Task Search_DonorWithAssociatedAntigen_10_10_ReturnsExpectedSearchResult()
    {
        var steps = GetSearchTestSteps(nameof(Search_DonorWithAssociatedAntigen_10_10_ReturnsExpectedSearchResult));

        const string testDescription = "10/10 Donor Search - Donor with Associated Antigen";
        steps.Logger.LogStart(testDescription);

        var expectedDonorCode = await steps.CreateDonor(ImportDonorType.Adult, ImportedHlaBuilder.AssociatedAntigenPhenotype);
        var searchResponse = await steps.SubmitSearchRequest("search-request-donor-associated-antigen-10_10.json");
        await steps.MatchingShouldReturnExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);
        await steps.SearchShouldReturnExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);

        steps.Logger.LogCompletion(testDescription);
    }

    private async Task Search_Patient(
        string testDescription,
        ImportDonorType importDonorType,
        string searchRequestJson,
        bool? parallelMatchPrediction,
        bool newDnaPhenotype = false,
        [CallerMemberName] string callerName = "")
    {
        await ExecuteWithRetry(async () =>
        {
            var steps = GetSearchTestSteps(callerName);
            steps.Logger.LogStart(testDescription);

            var hlaBuilder = newDnaPhenotype
                ? ImportedHlaBuilder.SearchNewPhenotype
                : ImportedHlaBuilder.SearchTestPhenotype;
            var expectedDonorCode = await steps.CreateDonor(importDonorType, hlaBuilder);
            var searchResponse = await steps.SubmitSearchRequest(searchRequestJson, parallelMatchPrediction);
            await steps.MatchingShouldReturnExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);
            await steps.SearchShouldReturnExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);

            steps.Logger.LogCompletion(testDescription);
        });
    }
}
