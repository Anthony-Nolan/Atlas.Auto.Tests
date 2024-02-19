using Atlas.Debug.Client.Models.DonorImport;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class DebugDonorsResultAssertions
    {
        public static void ShouldHaveExpectedDonors(this DebugDonorsResult debugResult, IReadOnlyCollection<DonorDebugInfo> expectedInfo)
        {
            var externalDonorCodes = expectedInfo.Select(d => d.ExternalDonorCode).ToList();

            debugResult.AllCodesShouldHaveBeenReceived(externalDonorCodes);

            debugResult.PresentDonors
                .Should().BeEquivalentTo(expectedInfo, "info for present donors should be as expected");

            debugResult.DonorCounts.Absent
                .Should().Be(0, "no donor codes should be absent");
        }

        public static void ShouldNotHaveTheseDonors(this DebugDonorsResult debugResult, IReadOnlyCollection<string> externalDonorCodes)
        {
            debugResult.AllCodesShouldHaveBeenReceived(externalDonorCodes);

            debugResult.AbsentDonors
                .Should().BeEquivalentTo(externalDonorCodes, "all donor codes should be absent");

            debugResult.DonorCounts.Present
                .Should().Be(0, "none of the donor codes should be present");
        }

        private static void AllCodesShouldHaveBeenReceived(this DebugDonorsResult debugResult, IEnumerable<string> externalDonorCodes)
        {
            debugResult.Should().NotBeNull("the debug result should not be null");

            debugResult.ReceivedDonors
                .Should().BeEquivalentTo(externalDonorCodes, "donor codes should have been received for checking");
        }
    }
}
