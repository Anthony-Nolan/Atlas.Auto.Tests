using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class DonorImportMessageAssertions
    {
        public static void ImportShouldHaveBeenSuccessful(this DonorImportMessage? message)
        {
            message.Should().NotBeNull();
            message!.WasSuccessful.Should().BeTrue();
            message.FailedImportInfo.Should().BeNull();
        }

        public static void ImportShouldHaveFailed(this DonorImportMessage? message)
        {
            message.Should().NotBeNull();
            message!.WasSuccessful.Should().BeFalse();
            message.FailedImportInfo.Should().NotBeNull();
            message.FailedImportInfo?.FileFailureReason.Should().Be(ImportFailureReason.ErrorDuringImport);
            message.SuccessfulImportInfo.Should().BeNull();
        }

        public static void ShouldHaveImportedDonorCount(this DonorImportMessage message, int expectedCount)
        {
            message.SuccessfulImportInfo?.ImportedDonorCount.Should().Be(expectedCount);
        }

        public static void ShouldHaveFailedDonorCount(this DonorImportMessage message, int expectedCount)
        {
            message.SuccessfulImportInfo?.FailedDonorCount.Should().Be(expectedCount);
        }
    }
}
