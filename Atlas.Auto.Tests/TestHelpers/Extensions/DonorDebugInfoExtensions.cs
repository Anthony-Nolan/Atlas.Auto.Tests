using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Auto.Tests.TestHelpers.Extensions
{
    internal static class DonorDebugInfoExtensions
    {
        public static IEnumerable<string> GetExternalDonorCodes(this IEnumerable<DonorDebugInfo> info)
        {
            return info.Select(d => d.ExternalDonorCode);
        }
    }
}
