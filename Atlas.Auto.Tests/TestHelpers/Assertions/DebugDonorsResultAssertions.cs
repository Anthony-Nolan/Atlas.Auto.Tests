using Atlas.Debug.Client.Models.DonorImport;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class DebugDonorsResultAssertions
    {
        public static void ShouldHaveAllExpectedDonors(this DebugDonorsResult debugDonorsResult, IReadOnlyCollection<DonorDebugInfo> expectedInfo)
        {
            debugDonorsResult.Should().NotBeNull("the debug result should not be null");

            var externalDonorCodes = expectedInfo.Select(d => d.ExternalDonorCode).ToList();

            debugDonorsResult.ReceivedDonors
                .Should().BeEquivalentTo(externalDonorCodes, "donor codes should have been received for checking");

            debugDonorsResult.PresentDonors.Select(d => d.ExternalDonorCode)
                .Should().BeEquivalentTo(externalDonorCodes, "all donor codes should have been present");
            debugDonorsResult.PresentDonors
                .Should().BeEquivalentTo(expectedInfo, "info for present donors should be as expected");

            debugDonorsResult.DonorCounts.Absent
                .Should().Be(0, "no donor codes should be absent");
        }
    }
}
