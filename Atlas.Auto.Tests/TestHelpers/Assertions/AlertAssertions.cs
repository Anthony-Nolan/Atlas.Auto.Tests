using Atlas.Client.Models.SupportMessages;
using FluentAssertions;

namespace Atlas.Auto.Tests.TestHelpers.Assertions
{
    internal static class AlertAssertions
    {
        public static void ShouldSayFullModeImportNotAllowed(this Alert alert)     
        {
            alert.Should().NotBeNull("the alert should have been fetched");
            alert.Summary.ToLower().Should().Contain("full mode is not allowed");
        }
    }
}
