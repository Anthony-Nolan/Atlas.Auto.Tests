using AventStack.ExtentReports;
using System.Collections.Concurrent;

namespace Atlas.Auto.Utils.Reporting
{
    public static class ExtentManager
    {
        private static ConcurrentDictionary<string, ExtentTest> parentTestMap = new ConcurrentDictionary<string, ExtentTest>();
        private static ConcurrentDictionary<string, ExtentTest> childTestMap = new ConcurrentDictionary<string, ExtentTest>();

        // Creates a parent node in the report for the test fixture
        public static ExtentTest CreateForFixture(string testFixtureName, string description = null)
        {
            var parentTest = new ThreadLocal<ExtentTest>();
            parentTest.Value = ExtentService.Instance.CreateTest(testFixtureName, description);
            parentTestMap.TryAdd(testFixtureName, parentTest.Value);
            return parentTest.Value;
        }

        // Creates a node in the report for the individual test
        public static ExtentTest CreateForTest(string parentName, string testName, string description = null)
        {
            ExtentTest parentTest = null;
            if (!parentTestMap.ContainsKey(parentName))
            {
                parentTest = ExtentService.Instance.CreateTest(testName);
                parentTestMap.TryAdd(parentName, parentTest);
            }
            else
            {
                parentTest = parentTestMap[parentName];
            }
            var childTest = new ThreadLocal<ExtentTest>();
            childTest.Value = parentTest.CreateNode(testName, description);
            childTestMap.TryAdd(testName, childTest.Value);
            return childTest.Value;
        }
    }
}
