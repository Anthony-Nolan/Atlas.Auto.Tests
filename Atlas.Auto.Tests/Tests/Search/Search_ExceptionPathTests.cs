using Atlas.Auto.Tests.TestHelpers.Assertions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.TestSteps;
using System.Runtime.CompilerServices;

namespace Atlas.Auto.Tests.Tests.Search;

[TestFixture]
[Category($"{nameof(Search_ExceptionPathTests)}")]
internal class Search_ExceptionPathTests : SearchTestBase
{
    public Search_ExceptionPathTests() : base(nameof(Search_ExceptionPathTests))
    {
    }

    [Test]
    public async Task Search_MissingRequiredInfo_ReturnsValidationError()
    {
        await RunTest("Search with missing required information", async test =>
        {
            var validationFailures = await test.Steps.SubmitInvalidSearchRequest("search-request-missing-locus-A.json");
            validationFailures.ToList().ShouldContain(@"'A' must not be empty.");
        });
    }

    [Test]
    public async Task Search_InvalidPatientHla_FailsDuringMatching()
    {
        await RunTest("Search with invalid patient HLA", async test =>
        {
            var searchResponse = await test.Steps.SubmitSearchRequest("search-request-invalid-patient-hla-at-A1.json");
            await test.Steps.MatchingShouldFailHlaValidation(searchResponse.SearchIdentifier);
        });
    }

    private async Task RunTest(
        string testDescription,
        Func<TestServices<ISearchTestSteps>, Task> action,
        [CallerMemberName] string callerName = "")
    {
        await ExecuteWithRetry(async () =>
        {
            var test = GetSearchTestServices(callerName);
            test.Logger.LogStart(testDescription);
            await action(test);
            test.Logger.LogCompletion(testDescription);
        });
    }
}
