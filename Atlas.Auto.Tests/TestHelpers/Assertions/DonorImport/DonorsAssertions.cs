using Atlas.Auto.Tests.TestHelpers.Data;
using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.Auto.Tests.TestHelpers.Extensions;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.DonorImport;

internal static class DonorsAssertions
{
    public static void ImportShouldHaveBeenSuccessful(this DonorImportMessage? message)
    {
        message.Should().NotBeNull("Import result message should have been received");
        message!.WasSuccessful.Should().BeTrue(
            "Import of file {0} should have been successful but got failure: {1}",
            message.FileName,
            message.FailedImportInfo?.FileFailureReason.ToString() ?? "unknown");
        message.FailedImportInfo.Should().BeNull(
            "Successful import of file {0} should not have failure info", message.FileName);
    }

    public static void ImportShouldHaveFailed(this DonorImportMessage? message)
    {
        message.Should().NotBeNull("Import result message should have been received");
        message!.WasSuccessful.Should().BeFalse(
            "Import of file {0} should have failed", message.FileName);
        message.FailedImportInfo.Should().NotBeNull(
            "Failed import of file {0} should have failure info", message.FileName);
        message.FailedImportInfo?.FileFailureReason.Should().Be(ImportFailureReason.ErrorDuringImport,
            "Failure reason for file {0} should be ErrorDuringImport", message.FileName);
        message.SuccessfulImportInfo.Should().BeNull(
            "Failed import of file {0} should not have success info", message.FileName);
    }

    public static void ShouldHaveImportedDonorCount(this DonorImportMessage message, int expectedCount)
    {
        message.SuccessfulImportInfo.Should().NotBeNull(
            "Successful import info should be present for file {0}", message.FileName);
        message.SuccessfulImportInfo!.ImportedDonorCount.Should().Be(expectedCount,
            "Imported donor count for file {0} should be {1}", message.FileName, expectedCount);
    }

    public static void ShouldHaveFailedDonorCount(this DonorImportMessage message, int expectedCount)
    {
        message.SuccessfulImportInfo.Should().NotBeNull(
            "Successful import info should be present for file {0}", message.FileName);
        message.SuccessfulImportInfo!.FailedDonorCount.Should().Be(expectedCount,
            "Failed donor count for file {0} should be {1}", message.FileName, expectedCount);
    }

    public static void ShouldHaveExpectedDonors<T>(
        this DonorCheckResult<T>? result,
        IReadOnlyCollection<DonorUpdate> expectedUpdates)
        where T : IDonorEntity
    {
        var codeList = string.Join(", ", expectedUpdates.Select(d => d.RecordId));
        result.Should().NotBeNull("Donor check result should have been returned for codes [{0}]", codeList);
        result!.AbsentDonors.Should().BeEmpty("No donors from [{0}] should be absent", codeList);
        result.PresentDonors.Should().HaveSameCount(expectedUpdates);

        foreach (var expected in expectedUpdates)
        {
            var actual = result.PresentDonors
                .SingleOrDefault(d => d.ExternalDonorCode == expected.RecordId);
            actual.Should().NotBeNull("Donor {0} should be present", expected.RecordId);
            actual!.DonorTypeName.Should().Be(expected.DonorType.ToString(),
                "DonorType mismatch for {0}", expected.RecordId);
            actual.RegistryCode.Should().Be(expected.RegistryCode,
                "RegistryCode mismatch for {0}", expected.RecordId);
            actual.EthnicityCode.Should().Be(expected.Ethnicity,
                "EthnicityCode mismatch for {0}", expected.RecordId);
            actual.GetHla().Should().BeEquivalentTo(expected.Hla?.ToPhenotypeInfoTransfer(),
                "HLA mismatch for {0}", expected.RecordId);
        }
    }

    public static void ShouldNotHaveTheseDonors<T>(
        this DonorCheckResult<T>? result,
        IReadOnlyCollection<string> externalDonorCodes)
    {
        var codeList = string.Join(", ", externalDonorCodes);
        result.Should().NotBeNull("Donor check result should have been returned for codes [{0}]", codeList);
        result!.AbsentDonors.Should().BeEquivalentTo(externalDonorCodes,
            "All donors [{0}] should be absent", codeList);
        result.PresentCount.Should().Be(0,
            "No donors from [{0}] should be present", codeList);
    }

    public static void ShouldContainFailureFor(
        this IReadOnlyCollection<HlaExpansionFailure> expansionFailures,
        string donorCode,
        string invalidHlaName)
    {
        expansionFailures.Should().NotBeNullOrEmpty(
            "HLA expansion failures should exist for donor {0} with invalid HLA {1}", donorCode, invalidHlaName);

        expansionFailures
            .Where(f => f.ExternalDonorCodes.Contains(donorCode) && f.InvalidHLA.EndsWith(invalidHlaName))
            .Should().NotBeNullOrEmpty(
                "HLA expansion failures should contain entry for donor {0} with invalid HLA ending with '{1}'. " +
                "Actual failures: [{2}]",
                donorCode,
                invalidHlaName,
                string.Join("; ", expansionFailures.Select(f =>
                    $"donors=[{string.Join(",", f.ExternalDonorCodes)}] hla={f.InvalidHLA}")));
    }
}
