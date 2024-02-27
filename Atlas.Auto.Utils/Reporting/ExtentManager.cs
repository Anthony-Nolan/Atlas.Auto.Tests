using AventStack.ExtentReports;
using System.Collections.Concurrent;

namespace Atlas.Auto.Utils.Reporting
{
    public static class ExtentManager
    {
        private static ConcurrentDictionary<string, ExtentTest> testFixtureMap = new ConcurrentDictionary<string, ExtentTest>();

        // Creates a parent node in the report for the test fixture
        public static ExtentTest CreateForFixture(string testFixtureName, string description = null)
        {
            return testFixtureMap.GetOrAdd(testFixtureName, ExtentService.Instance.CreateTest(testFixtureName, description));
        }

        // Creates a node in the report for the individual test
        public static ExtentTest CreateForTest(string testFixtureName, string testName, string description = null)
        {
            var parentTest = testFixtureMap.GetOrAdd(testFixtureName, ExtentService.Instance.CreateTest(testFixtureName));
            return parentTest.CreateNode(testName, description);
        }
    }
}
