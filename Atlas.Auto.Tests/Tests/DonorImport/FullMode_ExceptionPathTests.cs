using Atlas.Auto.Tests.DependencyInjection;
using Atlas.Auto.Tests.TestHelpers.SourceData;
using Atlas.Auto.Tests.TestHelpers.Workflows;
using FluentAssertions;

namespace Atlas.Auto.Tests.Tests.DonorImport
{
    /// <summary>
    /// Tests that cover exception paths of Atlas donor import when in Full mode.
    /// </summary>
    [TestFixture]
    [Category($"{TestConstants.DonorImportTestTag}_{nameof(FullMode_ExceptionPathTests)}")]
    // ReSharper disable once InconsistentNaming
    internal class FullMode_ExceptionPathTests
    {
        private IServiceProvider serviceProvider;
        private IDonorImportWorkflow donorImportWorkflow;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            serviceProvider = ServiceConfiguration.CreateProvider();
        }

        [SetUp]
        public void SetUp()
        {
            donorImportWorkflow = serviceProvider.ResolveServiceOrThrow<IDonorImportWorkflow>();
        }

        [Test]
        public async Task DonorImport_DoesNotAllowFullModeImport()
        {
            var response = await donorImportWorkflow.IsFullModeImportAllowed();

            // The debug http response should have been successful,
            // but the embedded result should be false as full mode import should not be allowed.
            response.WasSuccess.Should().BeTrue();
            response.DebugResult.Should().BeFalse();
        }
    }
}
