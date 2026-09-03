using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Client.Models.Search.Results;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal abstract class SearchTestStepsBase
{
    public ITestLogger Logger => logger;

    protected readonly ITestLogger logger;
    protected readonly string testName;
    protected readonly IDonorImportStepsForSearchTests donorImportSteps;

    protected SearchTestStepsBase(
        IDonorImportStepsForSearchTests donorImportSteps,
        ITestLogger logger,
        string testName)
    {
        this.donorImportSteps = donorImportSteps;
        this.logger = logger;
        this.testName = testName;
    }

    protected T AssertNotNull<T>(T? value, string because, string actionDescription) where T : class
    {
        logger.AssertThenLogAndThrow(
            () => value.Should().NotBeNull(because),
            actionDescription);
        return value!;
    }

    protected async Task DonorResultShouldBeAsExpected<TResult>(
        TResult? donorResult, string approvalFileNameSuffix)
        where TResult : Result
    {
        logger.AssertThenLogAndThrow(() => donorResult.Should().NotBeNull(), "Select donor result");

        await logger.AssertThenLogAndThrowAsync(
            () => VerifyJson(donorResult.SerializeSingle())
                .IgnoreVaryingSearchResultProperties()
                .WriteReceivedToApprovalsFolder($"{testName}_{approvalFileNameSuffix}"),
            $"Comparison of donor {donorResult!.DonorCode} to approved result");
    }
}
