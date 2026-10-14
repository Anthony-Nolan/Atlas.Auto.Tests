using Atlas.Debug.Client.Models.DonorImport;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Extensions
{
    internal static class DonorUpdateExtensions
    {
        public static IReadOnlyCollection<string> GetExternalDonorCodes(this IEnumerable<DonorUpdate> updates)
        {
            return updates.Select(u => u.RecordId).ToList();
        }

        public static IEnumerable<DonorDebugInfo> ToDonorDebugInfo(this IEnumerable<DonorUpdate> updates)
        {
            return updates.Select(ToDonorDebugInfo);
        }

        public static DonorDebugInfo ToDonorDebugInfo(this DonorUpdate update)
        {
            var (externalDonorCode, donorType, registryCode, ethnicityCode) = GetCommonFields(update);
            return new DonorDebugInfo
            {
                ExternalDonorCode = externalDonorCode,
                DonorType = donorType,
                RegistryCode = registryCode,
                EthnicityCode = ethnicityCode,
                Hla = update.Hla.ToPhenotypeInfoTransfer()
            };
        }

        public static IEnumerable<FailedDonorUpdate> ToFailureInfo(
            this IEnumerable<DonorUpdate> updates,
            string failedPropertyName,
            string failureReason)
        {
            return updates.Select(u => u.ToFailureInfo(failedPropertyName, failureReason));
        }

        public static FailedDonorUpdate ToFailureInfo(this DonorUpdate update, string failedPropertyName, string failureReason)
        {
            var (externalDonorCode, donorType, registryCode, ethnicityCode) = GetCommonFields(update);
            return new FailedDonorUpdate
            {
                ExternalDonorCode = externalDonorCode,
                DonorType = donorType,
                RegistryCode = registryCode,
                EthnicityCode = ethnicityCode,
                PropertyName = failedPropertyName,
                FailureReason = failureReason
            };
        }

        private static (string ExternalDonorCode, string DonorType, string RegistryCode, string EthnicityCode) GetCommonFields(
            DonorUpdate update) =>
            (update.RecordId, update.DonorType.ToString(), update.RegistryCode, update.Ethnicity);
    }
}
