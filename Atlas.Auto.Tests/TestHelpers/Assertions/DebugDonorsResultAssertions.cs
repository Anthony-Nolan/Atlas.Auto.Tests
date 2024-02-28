using Atlas.Debug.Client.Models.DonorImport;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions;

internal static class DebugDonorsResultAssertions
{
    public static void ShouldHaveExpectedDonors(this DebugDonorsResult? debugResult, IReadOnlyCollection<DonorDebugInfo> expectedInfo)
    {
        var externalDonorCodes = expectedInfo.Select(d => d.ExternalDonorCode).ToList();
        debugResult.AllCodesShouldHaveBeenReceived(externalDonorCodes);
        debugResult!.PresentDonors.Should().BeEquivalentTo(expectedInfo);
        debugResult.DonorCounts.Absent.Should().Be(0);
    }

    public static void ShouldNotHaveTheseDonors(this DebugDonorsResult? debugResult, IReadOnlyCollection<string> externalDonorCodes)
    {
        debugResult.AllCodesShouldHaveBeenReceived(externalDonorCodes);
        debugResult!.AbsentDonors.Should().BeEquivalentTo(externalDonorCodes);
        debugResult.DonorCounts.Present.Should().Be(0);
    }

    private static void AllCodesShouldHaveBeenReceived(this DebugDonorsResult? debugResult, IEnumerable<string> externalDonorCodes)
    {
        debugResult.Should().NotBeNull();
        debugResult!.ReceivedDonors.Should().BeEquivalentTo(externalDonorCodes);
    }
}