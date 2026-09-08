using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Logging;
using Atlas.Client.Models.Search.Results;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal abstract class SearchTestStepsBase
{
    public ITestLogger Logger => _logger;
    internal DonorImportStepsForSearchTests DonorImportSteps => _donorImportSteps;

    protected readonly ITestLogger _logger;
    protected readonly string _testName;
    protected readonly DonorImportStepsForSearchTests _donorImportSteps;

    protected SearchTestStepsBase(
        DonorImportStepsForSearchTests donorImportSteps,
        ITestLogger logger,
        string testName)
    {
        _donorImportSteps = donorImportSteps;
        _logger = logger;
        _testName = testName;
    }

    protected static T AssertNotNull<T>(T? value, string because) where T : class
    {
        value.Should().NotBeNull(because);
        return value!;
    }

    protected async Task DonorResultShouldBeAsExpected<TResult>(
        TResult? donorResult, string approvalFileNameSuffix)
        where TResult : Result
    {
        var result = AssertNotNull(donorResult, "Donor result should have been returned");

        await VerifyJson(result.SerializeSingle())
            .IgnoreVaryingSearchResultProperties()
            .WriteReceivedToApprovalsFolder($"{_testName}_{approvalFileNameSuffix}");
    }
}
