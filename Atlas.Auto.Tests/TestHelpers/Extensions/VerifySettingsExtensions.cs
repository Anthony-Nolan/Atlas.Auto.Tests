namespace Atlas.Auto.Tests.TestHelpers.Extensions
{
    internal static class VerifySettingsExtensions
    {
        public static SettingsTask WriteReceivedToApprovalsFolder(this SettingsTask settings, string fileName)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var approvalsDirectory = Path.Combine(currentDirectory, @"TestHelpers/Assertions/Approvals");
            approvalsDirectory = Path.GetFullPath(approvalsDirectory);
            settings.UseDirectory(approvalsDirectory);
            settings.UseFileName(fileName);
            settings.DisableRequireUniquePrefix();
            return settings;
        }

        public static SettingsTask IgnoreVaryingSearchResultProperties(this SettingsTask settings)
        {
            // these are properties that will vary between test runs/installations and should not be included in the approval
            var propertyNames = new[]
            {
                // ignore donor ids
                "DonorCode",
                "ExternalDonorCode", 
                "AtlasDonorId",
                "DonorId",
                // ignore HF set id
                "Id",
                // Ignore scores as weightings assigned to grades and confidences could differ between Atlas installations
                "GradeScore",
                "ConfidenceScore",
                "MatchGradeScore",
                "MatchConfidenceScore"
            };
            settings.IgnoreMembers(propertyNames);
            return settings;
        }
    }
}
