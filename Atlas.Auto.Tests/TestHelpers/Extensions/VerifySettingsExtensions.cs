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
            return settings;
        }

        public static SettingsTask IgnoreVaryingSearchResultProperties(this SettingsTask settings)
        {
            // these are properties that will vary between test runs and should not be included in the approval
            // Donor ids and the `Id` for the referenced HF set
            var propertyNames = new[] { "DonorCode", "ExternalDonorCode", "AtlasDonorId", "Id" };
            settings.IgnoreMembers(propertyNames);
            return settings;
        }
    }
}
