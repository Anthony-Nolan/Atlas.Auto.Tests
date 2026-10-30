using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.Services;
using Atlas.Auto.Tests.TestHelpers.Settings;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlas.Auto.Tests.TestHelpers.TestSteps;

/// <summary>
/// Steps that check the precomputed genotype sets of a donor (ATL-221): the rows the donor import writes, and the
/// search usage event that says whether match prediction read them.
/// </summary>
internal class PrecomputedGenotypeSetTestSteps
{
    private readonly ILogger _logger;
    private readonly IDonorStoreSqlHelper _sqlHelper;
    private readonly AppInsightsHelper _appInsightsHelper;
    private readonly PollyRetry _pollyRetry;
    private readonly RetrySettings _retry;

    public PrecomputedGenotypeSetTestSteps(IServiceProvider provider, ILogger logger)
    {
        _logger = logger;
        _sqlHelper = provider.GetRequiredService<IDonorStoreSqlHelper>();
        _appInsightsHelper = provider.GetRequiredService<AppInsightsHelper>();
        _pollyRetry = provider.GetRequiredService<PollyRetry>();
        _retry = provider.GetRequiredService<RetrySettings>();
    }

    public async Task<int> GetMatchingDonorId(string donorCode)
    {
        var result = await _sqlHelper.CheckDonorsInMatchingAlgorithm([donorCode]);
        result.PresentDonors.Should().ContainSingle($"donor {donorCode} should be available for search");
        return result.PresentDonors.Single().DonorId;
    }

    /// <summary>
    /// Waits for the donor to have a row for each allowed-loci combination, then checks each row points at a value for
    /// that combination and the donor's frequency set. The import writes the rows after the donor is committed, so they
    /// can come a little later than the donor.
    /// </summary>
    /// <param name="previousSets">The donor's rows before its HLA was changed: every typing key should now be different.</param>
    public async Task<IReadOnlyCollection<PrecomputedGenotypeSet>> DonorShouldHavePrecomputedGenotypeSets(
        int donorId,
        IReadOnlyCollection<PrecomputedGenotypeSet>? previousSets = null)
    {
        var allowedLociKeys = Enum.GetValues<AllowedLociKey>();
        var previousTypingKeys = previousSets?.Select(s => s.HlaTypingKey).ToHashSet() ?? [];

        var sets = await _pollyRetry.ExecuteWithRetry(async () =>
            {
                var found = await _sqlHelper.GetPrecomputedGenotypeSets(donorId);
                var complete = found.Count == allowedLociKeys.Length && found.All(s => !previousTypingKeys.Contains(s.HlaTypingKey));
                return complete ? found : null;
            },
            _retry.CheckDonorsInMatching, $"Fetch precomputed genotype sets of donor {donorId}");

        // The last read, so a failure shows what is there rather than only that the wait ran out
        sets ??= await _sqlHelper.GetPrecomputedGenotypeSets(donorId);
        var frequencySetId = await _sqlHelper.GetActiveHaplotypeFrequencySetId(TestConstants.DefaultRegistryCode, TestConstants.DefaultEthnicity);

        using (new AssertionScope())
        {
            frequencySetId.Should().NotBeNull("the autotest haplotype frequency set should be loaded and active");
            sets.Select(s => s.AllowedLociKey).Should().BeEquivalentTo(allowedLociKeys,
                "donor {0} should have one row per allowed-loci combination", donorId);

            foreach (var set in sets)
            {
                set.ValueAllowedLociKey.Should().Be(set.AllowedLociKey, "the {0} row should point at a {0} value", set.AllowedLociKey);
                set.HaplotypeFrequencySetId.Should().Be(frequencySetId, "the {0} value should use the donor's frequency set", set.AllowedLociKey);
                set.HasData.Should().Be(!set.IsUnrepresented, "the {0} value should have data unless it is unrepresented", set.AllowedLociKey);
                previousTypingKeys.Should().NotContain(set.HlaTypingKey, "the {0} value should be for the donor's new typing", set.AllowedLociKey);
            }
        }

        _logger.LogInformation("Donor {DonorId} has precomputed genotype sets: {Sets}", donorId, string.Join(", ", sets));
        return sets;
    }

    public async Task PrecomputedGenotypeSetsShouldHaveBeenUsed(string searchRequestId)
    {
        // App Insights takes a few minutes to ingest an event
        var usage = await _pollyRetry.ExecuteWithRetry(
            async () => await _appInsightsHelper.GetPrecomputedGenotypeSetUsage(searchRequestId),
            _retry.FetchAppInsightsEvents, $"Fetch precomputed genotype set usage of search request '{searchRequestId}'");

        usage.Should().NotBeNull("match prediction should send a usage event for search request {0}", searchRequestId);
        _logger.LogInformation("Precomputed genotype set usage: {Usage}", usage);

        // Environment setting, not a defect: ForceLive ignores the request. Warn and let the test go on to its next steps.
        if (usage!.PrecomputeModes.Split(',').Contains("ForceLive"))
        {
            Assert.Warn($"Precompute mode is '{usage.PrecomputeModes}' on this environment, so the search could not use " +
                        "precomputed genotype sets. Set it to DefaultLive or DefaultPrecomputed to check them.");
            return;
        }

        using (new AssertionScope())
        {
            usage.UsePrecomputedGenotypeSets.Should().Be(bool.TrueString,
                "the search asked for precomputed genotype sets; precompute mode was '{0}'", usage.PrecomputeModes);
            usage.PrecomputedCount.Should().BeGreaterThanOrEqualTo(1,
                "at least the imported donor should be read from its precomputed genotype set; precompute mode was '{0}'", usage.PrecomputeModes);
        }
    }
}
