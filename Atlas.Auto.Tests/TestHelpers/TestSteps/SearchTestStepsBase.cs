using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.Auto.Tests.TestHelpers.InternalModels;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Client.Models.Search.Results;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

internal abstract class SearchTestStepsBase
{
    public ILogger Logger => _logger;
    internal DonorImportStepsForSearchTests DonorImportSteps => _donorImportSteps;

    protected readonly ILogger _logger;
    protected readonly string _testName;
    protected readonly DonorImportStepsForSearchTests _donorImportSteps;
    protected readonly PublicApiClient _publicApiClient;
    protected readonly BlobStorageHelper _blobHelper;
    protected readonly PollyRetry _pollyRetry;
    protected readonly RetrySettings _retry;

    protected SearchTestStepsBase(
        IServiceProvider provider,
        DonorImportStepsForSearchTests donorImportSteps,
        ILogger logger,
        string testName)
    {
        _donorImportSteps = donorImportSteps;
        _logger = logger;
        _testName = testName;
        _publicApiClient = provider.GetRequiredService<PublicApiClient>();
        _blobHelper = provider.GetRequiredService<BlobStorageHelper>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
    }

    public async Task<string> CreateDonor(ImportDonorType donorType, Builder<ImportedHla> hlaBuilder)
    {
        return await _donorImportSteps.CreateDonor(donorType, hlaBuilder);
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
