using Atlas.Auto.Tests.TestHelpers.Services;

namespace Atlas.Auto.Tests.TestHelpers.InternalModels
{
    internal class TestServices<T>
    {
        public T Steps { get; set; }
        public ITestLogger Logger { get; set; }

        public TestServices(T steps, ITestLogger logger)
        {
            Steps = steps;
            Logger = logger;
        }
    }
}
