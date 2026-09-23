using Atlas.Auto.Tests.TestHelpers.Data.Entities;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Extensions
{
    internal static class DonorUpdateExtensions
    {
        public static IReadOnlyCollection<string> GetExternalDonorCodes(this IEnumerable<DonorUpdate> updates)
        {
            return updates.Select(u => u.RecordId).ToList();
        }

        public static IEnumerable<DonorImportFailure> ToExpectedFailures(
            this IEnumerable<DonorUpdate> updates,
            string failedPropertyName,
            string failureReason)
        {
            return updates.Select(u => u.ToExpectedFailure(failedPropertyName, failureReason));
        }

        public static DonorImportFailure ToExpectedFailure(this DonorUpdate update, string failedPropertyName, string failureReason)
        {
            return new DonorImportFailure
            {
                ExternalDonorCode = update.RecordId,
                DonorType = update.DonorType.ToString(),
                RegistryCode = update.RegistryCode,
                EthnicityCode = update.Ethnicity,
                UpdateProperty = failedPropertyName,
                FailureReason = failureReason
            };
        }
    }
}
