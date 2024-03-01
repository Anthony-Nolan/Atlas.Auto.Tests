using Atlas.Debug.Client.Models.DonorImport;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.DonorImport;

internal static class DonorImportFailureInfoAssertions
{
    public static void ShouldBeEquivalentTo(this DonorImportFailureInfo failureInfo, string fileName, IReadOnlyCollection<FailedDonorUpdate> expectedFailedUpdates)
    {
        failureInfo.Should().NotBeNull();
        failureInfo.FileName.Should().Be(fileName);
        failureInfo.FailedUpdateCount.Should().Be(expectedFailedUpdates.Count);
        failureInfo.FailedUpdates.Should().BeEquivalentTo(expectedFailedUpdates);
    }
}