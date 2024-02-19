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
            return new DonorDebugInfo
            {
                ExternalDonorCode = update.RecordId,
                DonorType = update.DonorType.ToString(),
                RegistryCode = update.RegistryCode,
                EthnicityCode = update.Ethnicity,
                Hla = update.Hla.ToPhenotypeInfoTransfer()
            };
        }
    }
}
