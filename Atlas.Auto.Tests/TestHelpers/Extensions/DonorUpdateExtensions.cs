using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.Auto.Tests.TestHelpers.Extensions
{
    internal static class DonorUpdateExtensions
    {
        public static IReadOnlyCollection<string> GetExternalDonorCodes(this IEnumerable<DonorUpdate> updates)
        {
            return updates.Select(u => u.RecordId).ToList();
        }
    }
}
