using Atlas.Debug.Client.Models.ApplicationInsights;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.DonorImport;

internal static class HlaExpansionFailureAssertions
{
    public static void ShouldContainFailureFor(
        this IReadOnlyCollection<HlaExpansionFailure> expansionFailures,
        string donorCode,
        string invalidHlaName)
    {
        expansionFailures.Should().NotBeNullOrEmpty();

        // important: `InvalidHla` in failure object will include the locus name
        // so have to find the failure that **ends with** the expected invalid HLA name
        expansionFailures
            .Where(f => f.ExternalDonorCodes.Contains(donorCode) && f.InvalidHLA.EndsWith(invalidHlaName))
            .Should().NotBeNullOrEmpty();
    }
}