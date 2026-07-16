using Atlas.DonorImport.FileSchema.Models;
using System.Runtime.CompilerServices;

namespace Atlas.Auto.Tests.Tests.Search;

[TestFixture]
[Category($"{nameof(Search_HappyPathTests)}")]
internal class Search_HappyPathTests : SearchTestBase
{
    public Search_HappyPathTests() : base(nameof(Search_HappyPathTests))
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
            var test = GetSearchTestServices(callerName);
            test.Logger.LogStart(testDescription);

            var expectedDonorCode = newDnaPhenotype
                ? await test.Steps.CreateDonorWithNew(importDonorType)
                : await test.Steps.CreateDonor(importDonorType);
            var searchResponse = await test.Steps.SubmitSearchRequest(searchRequestJson, parallelMatchPrediction);
            await test.Steps.MatchingShouldReturnExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);
            await test.Steps.SearchShouldReturnExpectedDonor(searchResponse.SearchIdentifier, expectedDonorCode);

            test.Logger.LogCompletion(testDescription);
        });
    }
}
