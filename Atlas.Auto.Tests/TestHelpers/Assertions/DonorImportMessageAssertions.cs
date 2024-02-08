﻿using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class DonorImportMessageAssertions
    {
        public static void ImportWasSuccessful(this DonorImportMessage message)
        {
            message.Should().NotBeNull("the result message should have been fetched");

            const string reason = "the file should have been imported successfully";
            message.WasSuccessful.Should().BeTrue(reason);
            message.FailedImportInfo.Should().BeNull(reason);
        }

        public static void ShouldHaveImportedDonorCount(this DonorImportMessage message, int expectedCount)
        {
            message.SuccessfulImportInfo?.ImportedDonorCount.Should().Be(expectedCount, $"Imported donor count should be {expectedCount}");
        }

        public static void ShouldHaveFailedDonorCount(this DonorImportMessage message, int expectedCount)
        {
            message.SuccessfulImportInfo?.FailedDonorCount.Should().Be(expectedCount, $"Failed donor count should be {expectedCount}");
        }
    }
}
