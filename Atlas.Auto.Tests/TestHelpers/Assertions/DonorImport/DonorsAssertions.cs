using Atlas.Debug.Client.Models.ApplicationInsights;
using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions.DonorImport;

internal static class DonorsAssertions
{
    public static void ImportShouldHaveBeenSuccessful(this DonorImportMessage? message)
    {
        message.Should().NotBeNull("import result message should have been received");
        message!.WasSuccessful.Should().BeTrue(
            "import of file {0} should have been successful but got failure: {1}",
            message.FileName,
            message.FailedImportInfo?.FileFailureReason.ToString() ?? "unknown");
        message.FailedImportInfo.Should().BeNull(
            "successful import of file {0} should not have failure info", message.FileName);
    }

    public static void ImportShouldHaveFailed(this DonorImportMessage? message)
    {
        message.Should().NotBeNull("import result message should have been received");
        message!.WasSuccessful.Should().BeFalse(
            "import of file {0} should have failed", message.FileName);
        message.FailedImportInfo.Should().NotBeNull(
            "failed import of file {0} should have failure info", message.FileName);
        message.FailedImportInfo?.FileFailureReason.Should().Be(ImportFailureReason.ErrorDuringImport,
            "failure reason for file {0} should be ErrorDuringImport", message.FileName);
        message.SuccessfulImportInfo.Should().BeNull(
            "failed import of file {0} should not have success info", message.FileName);
    }

    public static void ShouldHaveImportedDonorCount(this DonorImportMessage message, int expectedCount)
    {
        message.SuccessfulImportInfo.Should().NotBeNull(
            "successful import info should be present for file {0}", message.FileName);
        message.SuccessfulImportInfo!.ImportedDonorCount.Should().Be(expectedCount,
            "imported donor count for file {0} should be {1}", message.FileName, expectedCount);
    }

    public static void ShouldHaveFailedDonorCount(this DonorImportMessage message, int expectedCount)
    {
        message.SuccessfulImportInfo.Should().NotBeNull(
            "successful import info should be present for file {0}", message.FileName);
        message.SuccessfulImportInfo!.FailedDonorCount.Should().Be(expectedCount,
            "failed donor count for file {0} should be {1}", message.FileName, expectedCount);
    }

    public static void ShouldHaveExpectedDonors(this DebugDonorsResult? debugResult, IReadOnlyCollection<DonorDebugInfo> expectedInfo)
    {
        var externalDonorCodes = expectedInfo.Select(d => d.ExternalDonorCode).ToList();
        var codeList = string.Join(", ", externalDonorCodes);
        debugResult.Should().NotBeNull("donor check result should have been returned for codes [{0}]", codeList);
        debugResult!.ReceivedDonors.Should().BeEquivalentTo(externalDonorCodes,
            "all requested donor codes [{0}] should have been received in the response", codeList);
        debugResult.PresentDonors.Should().BeEquivalentTo(expectedInfo,
            "present donors should match expected info for codes [{0}]", codeList);
        debugResult.DonorCounts.Absent.Should().Be(0,
            "no donors from [{0}] should be absent", codeList);
    }

    public static void ShouldNotHaveTheseDonors(this DebugDonorsResult? debugResult, IReadOnlyCollection<string> externalDonorCodes)
    {
        var codeList = string.Join(", ", externalDonorCodes);
        debugResult.Should().NotBeNull("donor check result should have been returned for codes [{0}]", codeList);
        debugResult!.ReceivedDonors.Should().BeEquivalentTo(externalDonorCodes,
            "all requested donor codes [{0}] should have been received in the response", codeList);
        debugResult.AbsentDonors.Should().BeEquivalentTo(externalDonorCodes,
            "all donors [{0}] should be absent", codeList);
        debugResult.DonorCounts.Present.Should().Be(0,
            "no donors from [{0}] should be present", codeList);
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
