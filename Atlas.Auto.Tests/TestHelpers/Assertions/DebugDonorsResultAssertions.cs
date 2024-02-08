using Atlas.Debug.Client.Models.DonorImport;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class DebugDonorsResultAssertions
    {
        public static void ShouldHaveAllExpectedDonors(this DebugDonorsResult debugDonorsResult, IReadOnlyCollection<string> externalDonorCodes)
        {
            debugDonorsResult.Should().NotBeNull("the debug result should not be null");
            debugDonorsResult.ReceivedDonors.Should().BeEquivalentTo(externalDonorCodes, "donor codes should have been received for checking");
            debugDonorsResult.PresentDonors.Select(d => d.ExternalDonorCode).Should().BeEquivalentTo(externalDonorCodes, "all donor codes should have been present");
            debugDonorsResult.DonorCounts.Absent.Should().Be(0, "no donor codes should be absent");
        }

        // todo #9: after `DonorDebugInfo` becomes `IEquatable`, assert that the info for all present donors is correct
    }
}
