using Atlas.Auto.Tests.TestHelpers.Assertions;
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
        await RunTest("Search with missing required information", async steps =>
        {
            var validationFailures = await steps.SubmitInvalidSearchRequest("search-request-missing-locus-A.json");
            validationFailures.ToList().ShouldContain(@"'A' must not be empty.");
        });
    }

    [Test]
    public async Task Search_InvalidPatientHla_FailsDuringMatching()
    {
        await RunTest("Search with invalid patient HLA", async steps =>
        {
            var searchResponse = await steps.SubmitSearchRequest("search-request-invalid-patient-hla-at-A1.json");
            await steps.MatchingShouldFailHlaValidation(searchResponse.SearchIdentifier);
        });
    }

    private async Task RunTest(
        string testDescription,
        Func<SearchTestSteps, Task> action,
        [CallerMemberName] string callerName = "")
    {
        await ExecuteWithRetry(async () =>
        {
            var steps = GetSearchTestSteps(callerName);
            steps.Logger.LogStart(testDescription);
            await action(steps);
            steps.Logger.LogCompletion(testDescription);
        });
    }
}
